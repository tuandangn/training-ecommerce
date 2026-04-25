using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Domain.Shared.Services.Catalog;

public interface IProductManager : INameExistCheckingService
{
    Task<IEnumerable<ProductDto>> GetProductsByIdsAsync(IEnumerable<Guid> ids);
    Task<IPagedDataDto<ProductDto>> GetProductsAsync(int pageIndex, int pageSize, string? keywords = null, Guid? categoryId = null);

    Task<IEnumerable<ProductDto>> GetProductsByVendorIdAsync(Guid vendorId);
    Task AddProductVendorAsync(Guid productId, Guid vendorId, int displayOrder);
    Task RemoveProductVendorAsync(Guid productId, Guid vendorId);
    
    Task<CreateProductResultDto> CreateProductAsync(CreateProductDto dto);
    Task<UpdateProductResultDto> UpdateProductAsync(UpdateProductDto dto);
    Task UpdateProductPriceAsync(UpdateProductPriceDto dto);
    Task DeleteProductAsync(Guid id);

    Task<IEnumerable<ProductPriceHistoryDto>> GetProductPriceHistoryAsync(Guid productId);
}