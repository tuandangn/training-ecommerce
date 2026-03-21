using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Domain.Shared.Services.Catalog;

public interface IVendorManager
{
    Task<IPagedDataDto<VendorDto>> GetVendorsAsync(string? keywords, int pageIndex, int pageSize);

    Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null);

    Task<CreateVendorResultDto> CreateVendorAsync(CreateVendorDto dto);

    Task<UpdateVendorResultDto> UpdateVendorAsync(UpdateVendorDto dto);

    Task DeleteVendorAsync(Guid id);
}
