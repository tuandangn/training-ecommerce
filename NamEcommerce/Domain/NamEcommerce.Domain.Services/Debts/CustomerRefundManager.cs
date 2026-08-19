using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Shared.Services.Debts;
using Microsoft.EntityFrameworkCore;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class CustomerRefundManager(
    IRepository<CustomerRefund> refundRepository,
    IEntityDataReader<CustomerRefund> refundReader,
    ICustomerLedgerManager customerLedgerManager,
    EntityCodeGenerator entityCodeGenerator) : ICustomerRefundManager
{
    private Task<string> GenerateCodeAsync()
    {
        var prefix = $"PC-KH-{DateTime.UtcNow:yyMM}";
        return entityCodeGenerator.NextAsync(prefix, () => refundReader.GetDataSource(new() { IncludeDeleted = true })
            .CountAsync(r => r.Code.StartsWith(prefix)));
    }

    public async Task<CustomerRefundDto> CreateAsync(CreateCustomerRefundDto dto)
    {
        dto.Verify();

        // Idempotency: đã có refund cho CustomerReturn này rồi thì trả về
        var existing = await refundReader.DataSource
            .FirstOrDefaultAsync(r => r.CustomerReturnId == dto.CustomerReturnId && r.Status != CustomerRefundStatus.Cancelled)
            .ConfigureAwait(false);
        if (existing != null)
            return MapToDto(existing);

        var code = await GenerateCodeAsync().ConfigureAwait(false);
        var customerReturn = await refundRepository.GetByIdAsync(dto.CustomerReturnId).ConfigureAwait(false)
            ?? throw new CustomerReturnNotFoundException(dto.CustomerReturnId);

        var refund = new CustomerRefund(
            code: code,
            customerId: dto.CustomerId,
            customerName: dto.CustomerName,
            customerReturnId: dto.CustomerReturnId,
            customerReturnCode: dto.CustomerReturnCode,
            customerDebtId: dto.CustomerDebtId,
            amount: dto.Amount,
            createdByUserId: customerReturn.CreatedByUserId);

        refund.MarkCreated();
        var inserted = await refundRepository.InsertAsync(refund).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<CustomerRefundDto> CompleteAsync(Guid id, PaymentMethod paymentMethod, Guid? bankAccountId, string? note, Guid? completedByUserId)
    {
        var refund = await refundRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerRefund.NotFound");

        refund.Complete(paymentMethod, bankAccountId, note, completedByUserId);
        await refundRepository.UpdateAsync(refund).ConfigureAwait(false);

        await customerLedgerManager.RecordRefundPayoutAsync(new RecordCustomerLedgerRefundPayoutDto
        {
            CustomerId = refund.CustomerId,
            Amount = refund.Amount,
            ReferenceId = refund.Id,
            ReferenceCode = refund.Code,
            OccurredAtUtc = refund.RefundedOnUtc ?? DateTime.UtcNow,
            CreatedByUserId = completedByUserId
        }).ConfigureAwait(false);

        return MapToDto(refund);
    }

    public async Task<CustomerRefundDto> CancelAsync(Guid id)
    {
        var refund = await refundRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CustomerRefund.NotFound");

        refund.Cancel();
        await refundRepository.UpdateAsync(refund).ConfigureAwait(false);
        return MapToDto(refund);
    }

    public async Task<CustomerRefundDto?> GetByIdAsync(Guid id)
    {
        var refund = await refundReader.GetByIdAsync(id).ConfigureAwait(false);
        return refund == null ? null : MapToDto(refund);
    }

    public async Task<IPagedDataDto<CustomerRefundDto>> GetListAsync(
        Guid? customerId = null, int? status = null, string? keywords = null,
        int pageIndex = 0, int pageSize = 15)
    {
        var query = refundReader.DataSource;

        if (customerId.HasValue)
            query = query.Where(r => r.CustomerId == customerId.Value);
        if (status.HasValue)
            query = query.Where(r => (int)r.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keywords))
            query = query.Where(r => r.Code.Contains(keywords)
                || r.CustomerName.Contains(keywords)
                || r.CustomerReturnCode.Contains(keywords));

        query = query.OrderByDescending(r => r.CreatedOnUtc);

        var total = await query.CountAsync().ConfigureAwait(false);
        var items = (await query.Skip(pageIndex * pageSize).Take(pageSize)
            .ToListAsync().ConfigureAwait(false))
            .Select(MapToDto)
            .ToList();

        return PagedDataDto.Create(items, pageIndex, pageSize, total);
    }

    private static CustomerRefundDto MapToDto(CustomerRefund r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        CustomerId = r.CustomerId,
        CustomerName = r.CustomerName,
        CustomerReturnId = r.CustomerReturnId,
        CustomerReturnCode = r.CustomerReturnCode,
        CustomerDebtId = r.CustomerDebtId,
        Amount = r.Amount,
        Status = r.Status,
        PaymentMethod = r.PaymentMethod,
        BankAccountId = r.BankAccountId,
        Note = r.Note,
        RefundedOnUtc = r.RefundedOnUtc,
        CompletedByUserId = r.CompletedByUserId,
        CreatedByUserId = r.CreatedByUserId,
        CreatedOnUtc = r.CreatedOnUtc,
        UpdatedOnUtc = r.UpdatedOnUtc
    };
}
