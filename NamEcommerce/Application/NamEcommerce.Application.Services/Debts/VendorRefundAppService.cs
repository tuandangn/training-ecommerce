using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Application.Services.Debts;

public sealed class VendorRefundAppService(IVendorRefundManager refundManager) : IVendorRefundAppService
{
    public async Task<VendorRefundAppDto?> GetByIdAsync(Guid id)
    {
        var dto = await refundManager.GetByIdAsync(id).ConfigureAwait(false);
        return dto == null ? null : MapToAppDto(dto);
    }

    public async Task<IPagedDataAppDto<VendorRefundAppDto>> GetListAsync(
        Guid? vendorId = null,
        int? status = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15)
    {
        var paged = await refundManager.GetListAsync(vendorId, status, keywords, pageIndex, pageSize)
            .ConfigureAwait(false);
        return PagedDataAppDto.Create(paged.Items.Select(MapToAppDto).ToList(),
            paged.PagerInfo.PageIndex, paged.PagerInfo.PageSize, paged.PagerInfo.TotalCount);
    }

    public async Task<CompleteVendorRefundResultAppDto> CompleteAsync(CompleteVendorRefundAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CompleteVendorRefundResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var result = await refundManager.CompleteAsync(
                dto.RefundId, (PaymentMethod)dto.PaymentMethod, dto.BankAccountId, dto.Note, dto.CompletedByUserId).ConfigureAwait(false);
            return new CompleteVendorRefundResultAppDto { Success = true, Refund = MapToAppDto(result) };
        }
        catch (Exception ex)
        {
            return new CompleteVendorRefundResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<CancelVendorRefundResultAppDto> CancelAsync(Guid id)
    {
        try
        {
            await refundManager.CancelAsync(id).ConfigureAwait(false);
            return new CancelVendorRefundResultAppDto { Success = true };
        }
        catch (Exception ex)
        {
            return new CancelVendorRefundResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static VendorRefundAppDto MapToAppDto(VendorRefundDto dto) => new()
    {
        Id = dto.Id,
        Code = dto.Code,
        VendorId = dto.VendorId,
        VendorName = dto.VendorName,
        VendorReturnId = dto.VendorReturnId,
        VendorReturnCode = dto.VendorReturnCode,
        VendorDebtId = dto.VendorDebtId,
        Amount = dto.Amount,
        Status = (int)dto.Status,
        PaymentMethod = (int?)dto.PaymentMethod,
        BankAccountId = dto.BankAccountId,
        Note = dto.Note,
        RefundedOnUtc = dto.RefundedOnUtc,
        CompletedByUserId = dto.CompletedByUserId,
        CreatedByUserId = dto.CreatedByUserId,
        CreatedOnUtc = dto.CreatedOnUtc,
        UpdatedOnUtc = dto.UpdatedOnUtc
    };
}
