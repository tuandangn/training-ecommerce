using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Catalog;

public interface IProductAppService
{
    Task<IPagedDataAppDto<ProductAppDto>> GetProductsAsync(int pageIndex, int pageSize, string? keywords = null, Guid? categoryId = null, Guid? vendorId = null);

    Task<ProductAppDto?> GetProductByIdAsync(Guid id);

    Task<IEnumerable<ProductAppDto>> GetProductsByIdsAsync(IEnumerable<Guid> ids);

    Task<IEnumerable<ProductAppDto>> GetProductsByVendorIdAsync(Guid vendorId);

    Task<CreateProductResultAppDto> CreateProductAsync(CreateProductAppDto dto);

    Task<UpdateProductResultAppDto> UpdateProductAsync(UpdateProductAppDto dto);

    Task<DeleteProductResultAppDto> DeleteProductAsync(DeleteProductAppDto dto);

    Task<IEnumerable<ProductPriceHistoryAppDto>> GetProductPriceHistoryAsync(Guid productId);
}