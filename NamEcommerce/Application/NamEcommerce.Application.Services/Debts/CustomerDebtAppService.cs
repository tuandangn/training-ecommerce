using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Application.Services.Debts;

public sealed class CustomerDebtAppService(ICustomerDebtManager debtManager) : ICustomerDebtAppService
{
    private readonly ICustomerDebtManager _debtManager = debtManager;

    public async Task<CustomerPaymentAppDto> RecordPaymentAsync(CreateCustomerPaymentAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            throw new Exception(errorMessage);

        var domainDto = new CreateCustomerPaymentDto
        {
            CustomerId = dto.CustomerId,
            OrderId = dto.OrderId,
            DeliveryNoteId = dto.DeliveryNoteId,
            CustomerDebtId = dto.CustomerDebtId,
            Amount = dto.Amount,
            PaymentMethod = (PaymentMethod)dto.PaymentMethod,
            PaymentType = (PaymentType)dto.PaymentType,
            Note = dto.Note,
            PaidOnUtc = dto.PaidOnUtc,
            RecordedByUserId = dto.RecordedByUserId
        };

        var result = await _debtManager.RecordPaymentAsync(domainDto).ConfigureAwait(false);
        return result.ToDto();
    }

    public async Task<CustomerDebtAppDto?> GetDebtByIdAsync(Guid id)
    {
        var result = await _debtManager.GetDebtByIdAsync(id).ConfigureAwait(false);
        return result?.ToDto();
    }

    public async Task<PagedDataAppDto<CustomerDebtAppDto>> GetDebtsAsync(Guid? customerId = null, int pageIndex = 0, int pageSize = 15)
    {
        var paged = await _debtManager.GetDebtsAsync(customerId, pageIndex, pageSize).ConfigureAwait(false);
        var mappedItems = paged.Items.Select(d => d.ToDto()).ToList();
        return (PagedDataAppDto<CustomerDebtAppDto>)PagedDataAppDto.Create(mappedItems, paged.PagerInfo.PageIndex, paged.PagerInfo.PageSize, paged.PagerInfo.TotalCount);
    }

    public async Task<PagedDataAppDto<CustomerPaymentAppDto>> GetPaymentsAsync(Guid? customerId = null, int pageIndex = 0, int pageSize = 15)
    {
        var paged = await _debtManager.GetPaymentsAsync(customerId, pageIndex, pageSize).ConfigureAwait(false);
        var mappedItems = paged.Items.Select(p => p.ToDto()).ToList();
        return (PagedDataAppDto<CustomerPaymentAppDto>)PagedDataAppDto.Create(mappedItems, paged.PagerInfo.PageIndex, paged.PagerInfo.PageSize, paged.PagerInfo.TotalCount);
    }
}
