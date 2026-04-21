using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Catalog;

public interface IVendorAppService
{
    Task<IPagedDataAppDto<VendorAppDto>> GetVendorsAsync(string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<VendorAppDto?> GetVendorByIdAsync(Guid id);
    Task<IEnumerable<VendorAppDto>> GetVendorsByIdsAsync(IEnumerable<Guid> ids);

    Task<CreateVendorResultAppDto> CreateVendorAsync(CreateVendorAppDto dto);

    Task<UpdateVendorResultAppDto> UpdateVendorAsync(UpdateVendorAppDto dto);

    Task<DeleteVendorResultAppDto> DeleteVendorAsync(DeleteVendorAppDto dto);
}
