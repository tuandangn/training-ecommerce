using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.Returns;
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

public sealed class VendorRefundManager(
    IRepository<VendorRefund> refundRepository,
    IEntityDataReader<VendorRefund> refundReader,
    IEntityDataReader<VendorReturn> vendorReturnReader,
    IVendorLedgerManager vendorLedgerManager,
    EntityCodeGenerator entityCodeGenerator) : IVendorRefundManager
{
    private Task<string> GenerateCodeAsync()
    {
        var prefix = $"PT-NCC-R-{DateTime.UtcNow:yyMM}";
        return entityCodeGenerator.NextAsync(prefix, () => refundReader.SecuredDataSource.CountAsync(r => r.Code.StartsWith(prefix)));
    }

    public async Task<VendorRefundDto> CreateAsync(CreateVendorRefundDto dto)
    {
        dto.Verify();

        var existing = await refundReader.DataSource
            .FirstOrDefaultAsync(r => r.VendorReturnId == dto.VendorReturnId
                              && r.Status != VendorRefundStatus.Cancelled)
            .ConfigureAwait(false);
        if (existing != null)
            return MapToDto(existing);

        var code = await GenerateCodeAsync().ConfigureAwait(false);
        var vendorReturn = await refundRepository.GetByIdAsync(dto.VendorReturnId).ConfigureAwait(false)
            ?? throw new VendorReturnNotFoundException(dto.VendorReturnId);

        var refund = new VendorRefund(
            code: code,
            vendorId: dto.VendorId,
            vendorName: dto.VendorName,
            vendorReturnId: dto.VendorReturnId,
            vendorReturnCode: dto.VendorReturnCode,
            vendorDebtId: dto.VendorDebtId,
            amount: dto.Amount,
            createdByUserId: vendorReturn.CreatedByUserId);

        refund.MarkCreated();
        var inserted = await refundRepository.InsertAsync(refund).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<VendorRefundDto> CompleteAsync(Guid id, PaymentMethod paymentMethod, Guid? bankAccountId, string? note, Guid? completedByUserId)
    {
        var refund = await refundRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.VendorRefund.NotFound");

        refund.Complete(paymentMethod, bankAccountId, note, completedByUserId);
        await refundRepository.UpdateAsync(refund).ConfigureAwait(false);

        await vendorLedgerManager.RecordRefundReceiptAsync(new RecordVendorLedgerRefundReceiptDto
        {
            VendorId = refund.VendorId,
            Amount = refund.Amount,
            ReferenceId = refund.Id,
            ReferenceCode = refund.Code,
            OccurredAtUtc = refund.RefundedOnUtc ?? DateTime.UtcNow,
            CreatedByUserId = completedByUserId
        }).ConfigureAwait(false);

        return MapToDto(refund);
    }

    public async Task<VendorRefundDto> CancelAsync(Guid id)
    {
        var refund = await refundRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.VendorRefund.NotFound");

        refund.Cancel();
        await refundRepository.UpdateAsync(refund).ConfigureAwait(false);
        return MapToDto(refund);
    }

    public async Task<VendorRefundDto?> GetByIdAsync(Guid id)
    {
        var refund = await refundRepository.GetByIdAsync(id).ConfigureAwait(false);
        return refund == null ? null : MapToDto(refund);
    }

    public async Task<IPagedDataDto<VendorRefundDto>> GetListAsync(
        Guid? vendorId = null, int? status = null,
        string? keywords = null, int pageIndex = 0,int pageSize = 15)
    {
        var query = refundReader.DataSource;

        if (vendorId.HasValue)
            query = query.Where(r => r.VendorId == vendorId.Value);
        if (status.HasValue)
            query = query.Where(r => (int)r.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keywords))
            query = query.Where(r => r.Code.Contains(keywords)
                || r.VendorName.Contains(keywords)
                || r.VendorReturnCode.Contains(keywords));

        query = query.OrderByDescending(r => r.CreatedOnUtc);

        var total = await query.CountAsync().ConfigureAwait(false);
        var items = (await query.Skip(pageIndex * pageSize).Take(pageSize)
            .ToListAsync().ConfigureAwait(false))
            .Select(MapToDto)
            .ToList();

        return PagedDataDto.Create(items, pageIndex, pageSize, total);
    }

    private static VendorRefundDto MapToDto(VendorRefund r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        VendorId = r.VendorId,
        VendorName = r.VendorName,
        VendorReturnId = r.VendorReturnId,
        VendorReturnCode = r.VendorReturnCode,
        VendorDebtId = r.VendorDebtId,
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
