using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface IVendorRefundAppService
{
    Task<VendorRefundAppDto?> GetByIdAsync(Guid id);

    Task<IPagedDataAppDto<VendorRefundAppDto>> GetListAsync(
        Guid? vendorId = null,
        int? status = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<CompleteVendorRefundResultAppDto> CompleteAsync(CompleteVendorRefundAppDto dto);

    Task<CancelVendorRefundResultAppDto> CancelAsync(Guid id);
}
