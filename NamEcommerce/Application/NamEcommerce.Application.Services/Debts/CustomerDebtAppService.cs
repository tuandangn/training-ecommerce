using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Application.Services.Debts;

public sealed class CustomerDebtAppService(ICustomerDebtManager debtManager) : ICustomerDebtAppService
{
    private readonly ICustomerDebtManager _debtManager = debtManager;

    public async Task<CustomerPaymentAppDto> RecordPaymentAsync(CreateCustomerPaymentAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            throw new NamEcommerceDomainException(errorMessage!);

        var domainDto = MapToDomainDto(dto);
        var result = await _debtManager.RecordPaymentAsync(domainDto).ConfigureAwait(false);
        return result.ToDto();
    }

    public async Task<IList<CustomerPaymentAppDto>> RecordFlexiblePaymentForCustomerAsync(CreateCustomerPaymentAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            throw new NamEcommerceDomainException(errorMessage!);

        var domainDto = MapToDomainDto(dto);
        var results = await _debtManager.RecordFlexiblePaymentForCustomerAsync(domainDto).ConfigureAwait(false);
        return results.Select(p => p.ToDto()).ToList();
    }

    public async Task<CustomerDebtAppDto?> GetDebtByIdAsync(Guid id)
    {
        var result = await _debtManager.GetDebtByIdAsync(id).ConfigureAwait(false);
        return result?.ToDto();
    }

    public async Task<CustomerPaymentAppDto?> GetPaymentByIdAsync(Guid paymentId)
    {
        var result = await _debtManager.GetPaymentByIdAsync(paymentId).ConfigureAwait(false);
        return result?.ToDto();
    }

    public async Task<CustomerDebtSummaryAppDto?> GetCustomerDebtSummaryAsync(Guid customerId)
    {
        var result = await _debtManager.GetCustomerDebtSummaryAsync(customerId).ConfigureAwait(false);
        if (result == null) return null;
        return new CustomerDebtSummaryAppDto
        {
            CustomerId = result.CustomerId,
            CustomerName = result.CustomerName,
            TotalDebtAmount = result.TotalDebtAmount,
            TotalPaidAmount = result.TotalPaidAmount,
            TotalRemainingAmount = result.TotalRemainingAmount,
            DepositBalance = result.DepositBalance,
            DebtCount = result.DebtCount
        };
    }

    public async Task<IPagedDataAppDto<CustomerDebtSummaryAppDto>> GetCustomersWithDebtsAsync(int pageIndex = 0, int pageSize = 15, string? keywords = null)
    {
        var paged = await _debtManager.GetCustomersWithDebtsAsync(pageIndex, pageSize, keywords).ConfigureAwait(false);
        var mappedItems = paged.Items.Select(s => new CustomerDebtSummaryAppDto
        {
            CustomerId = s.CustomerId,
            CustomerName = s.CustomerName,
            CustomerPhone = s.CustomerPhone,
            CustomerAddress = s.CustomerAddress,
            TotalDebtAmount = s.TotalDebtAmount,
            TotalPaidAmount = s.TotalPaidAmount,
            TotalRemainingAmount = s.TotalRemainingAmount,
            DepositBalance = s.DepositBalance,
            DebtCount = s.DebtCount
        }).ToList();
        return PagedDataAppDto.Create(mappedItems, paged.PagerInfo.PageIndex, paged.PagerInfo.PageSize, paged.PagerInfo.TotalCount);
    }

    public async Task<CustomerDebtsByCustomerAppDto?> GetDebtsByCustomerIdAsync(Guid customerId)
    {
        var result = await _debtManager.GetDebtsByCustomerIdAsync(customerId).ConfigureAwait(false);
        if (result == null) return null;
        return new CustomerDebtsByCustomerAppDto
        {
            CustomerId = result.CustomerId,
            CustomerName = result.CustomerName,
            TotalDebtAmount = result.TotalDebtAmount,
            TotalPaidAmount = result.TotalPaidAmount,
            TotalRemainingAmount = result.TotalRemainingAmount,
            DepositBalance = result.DepositBalance,
            Debts = result.Debts.Select(d => d.ToDto()).ToList(),
            Deposits = result.Deposits.Select(p => p.ToDto()).ToList(),
            RecentPayments = result.RecentPayments.Select(p => p.ToDto()).ToList(),
            UnappliedCreditNoteBalance = result.UnappliedCreditNoteBalance,
            UnappliedCreditNotes = result.UnappliedCreditNotes.Select(c => c.ToDto()).ToList()
        };
    }

    public async Task<IPagedDataAppDto<CustomerDebtAppDto>> GetDebtsAsync(int pageIndex = 0, int pageSize = 15, Guid? customerId = null, string? keywords = null)
    {
        var paged = await _debtManager.GetDebtsAsync(pageIndex, pageSize, customerId, keywords).ConfigureAwait(false);
        var mappedItems = paged.Items.Select(d => d.ToDto()).ToList();
        return PagedDataAppDto.Create(mappedItems, paged.PagerInfo.PageIndex, paged.PagerInfo.PageSize, paged.PagerInfo.TotalCount);
    }

    public async Task<IPagedDataAppDto<CustomerPaymentAppDto>> GetPaymentsAsync(
        int pageIndex = 0, int pageSize = 15, 
        Guid? customerId = null, Guid? orderId = null)
    {
        var pagedData = await _debtManager.GetPaymentsAsync(pageIndex, pageSize, customerId, orderId)
            .ConfigureAwait(false);

        var mappedItems = pagedData.Items.Select(p => p.ToDto()).ToList();
        return PagedDataAppDto.Create(mappedItems, pagedData.PagerInfo.PageIndex, pagedData.PagerInfo.PageSize, pagedData.PagerInfo.TotalCount);
    }

    public Task<decimal> GetTotalPaidByOrderAsync(Guid orderId)
        => _debtManager.GetTotalPaidByOrderAsync(orderId);

    public Task<decimal> GetTotalDebtByOrderAsync(Guid orderId)
        => _debtManager.GetTotalDebtByOrderAsync(orderId);

    public async Task<CreateInitialCustomerDebtResultAppDto> CreateInitialDebtAsync(CreateInitialCustomerDebtAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CreateInitialCustomerDebtResultAppDto { Success = false, ErrorMessage = errorMessage };

        var domainDto = MapToDomainDto(dto);
        var result = await _debtManager.CreateInitialDebtAsync(domainDto).ConfigureAwait(false);
        return new CreateInitialCustomerDebtResultAppDto
        {
            Success = true,
            Debt = result.ToDto()
        };
    }
    private static CreateInitialCustomerDebtDto MapToDomainDto(CreateInitialCustomerDebtAppDto dto)
        => new()
        {
            CustomerId = dto.CustomerId,
            TotalAmount = dto.TotalAmount
        };

    private static CreateCustomerPaymentDto MapToDomainDto(CreateCustomerPaymentAppDto dto)
        => new()
        {
            CustomerId = dto.CustomerId,
            OrderId = dto.OrderId,
            DeliveryNoteId = dto.DeliveryNoteId,
            CustomerDebtId = dto.CustomerDebtId,
            Amount = dto.Amount,
            PaymentMethod = (PaymentMethod)dto.PaymentMethod,
            PaymentType = (PaymentType)dto.PaymentType,
            BankAccountId = dto.BankAccountId,
            Note = dto.Note,
            PaidOnUtc = dto.PaidOnUtc,
            RecordedByUserId = dto.RecordedByUserId
        };
}
