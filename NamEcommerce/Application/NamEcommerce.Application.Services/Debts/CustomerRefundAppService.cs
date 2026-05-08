using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Application.Services.Debts;

public sealed class CustomerRefundAppService(ICustomerRefundManager refundManager) : ICustomerRefundAppService
{
    public async Task<CustomerRefundAppDto?> GetByIdAsync(Guid id)
    {
        var dto = await refundManager.GetByIdAsync(id).ConfigureAwait(false);
        return dto == null ? null : MapToAppDto(dto);
    }

    public async Task<IPagedDataAppDto<CustomerRefundAppDto>> GetListAsync(
        Guid? customerId = null,
        int? status = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15)
    {
        var paged = await refundManager.GetListAsync(customerId, status, keywords, pageIndex, pageSize)
            .ConfigureAwait(false);
        return PagedDataAppDto.Create(paged.Items.Select(MapToAppDto).ToList(),
            paged.PagerInfo.PageIndex, paged.PagerInfo.PageSize, paged.PagerInfo.TotalCount);
    }

    public async Task<CompleteCustomerRefundResultAppDto> CompleteAsync(CompleteCustomerRefundAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CompleteCustomerRefundResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var result = await refundManager.CompleteAsync(
                dto.RefundId, (PaymentMethod)dto.PaymentMethod, dto.Note, dto.CompletedByUserId).ConfigureAwait(false);
            return new CompleteCustomerRefundResultAppDto { Success = true, Refund = MapToAppDto(result) };
        }
        catch (Exception ex)
        {
            return new CompleteCustomerRefundResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<CancelCustomerRefundResultAppDto> CancelAsync(Guid id)
    {
        try
        {
            await refundManager.CancelAsync(id).ConfigureAwait(false);
            return new CancelCustomerRefundResultAppDto { Success = true };
        }
        catch (Exception ex)
        {
            return new CancelCustomerRefundResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static CustomerRefundAppDto MapToAppDto(CustomerRefundDto dto) => new()
    {
        Id = dto.Id,
        Code = dto.Code,
        CustomerId = dto.CustomerId,
        CustomerName = dto.CustomerName,
        CustomerReturnId = dto.CustomerReturnId,
        CustomerReturnCode = dto.CustomerReturnCode,
        CustomerDebtId = dto.CustomerDebtId,
        Amount = dto.Amount,
        Status = (int) dto.Status,
        PaymentMethod = (int?) dto.PaymentMethod,
        Note = dto.Note,
        RefundedOnUtc = dto.RefundedOnUtc,
        CompletedByUserId = dto.CompletedByUserId,
        CreatedByUserId = dto.CreatedByUserId,
        CreatedOnUtc = dto.CreatedOnUtc,
        UpdatedOnUtc = dto.UpdatedOnUtc
    };
}
