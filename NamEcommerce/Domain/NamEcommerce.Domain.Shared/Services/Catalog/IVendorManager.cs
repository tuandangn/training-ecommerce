using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Domain.Shared.Services.Catalog;

public interface IVendorManager : IExistCheckingService
{
    Task<IPagedDataDto<VendorDto>> GetVendorsAsync(string? keywords, int pageIndex, int pageSize);

    Task<CreateVendorResultDto> CreateVendorAsync(CreateVendorDto dto);

    Task<UpdateVendorResultDto> UpdateVendorAsync(UpdateVendorDto dto);

    Task DeleteVendorAsync(Guid id);
}
