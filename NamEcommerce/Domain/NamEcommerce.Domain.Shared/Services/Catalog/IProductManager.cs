using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Domain.Shared.Services.Catalog;

public interface IProductManager : INameExistCheckingService
{
    Task<IPagedDataDto<ProductDto>> GetProductsAsync(
        int pageIndex, int pageSize,
        string? keywords = null,
        Guid? categoryId = null);

    Task<IList<ProductDto>> GetProductsByVendorIdAsync(Guid vendorId);
    Task AddProductVendorAsync(Guid productId, Guid vendorId, int displayOrder);
    Task RemoveProductVendorAsync(Guid productId, Guid vendorId);
    
    Task<CreateProductResultDto> CreateProductAsync(CreateProductDto dto);

    Task<UpdateProductResultDto> UpdateProductAsync(UpdateProductDto dto);

    Task DeleteProductAsync(Guid id);

    Task<IList<ProductDto>> GetProductsByIdsAsync(IEnumerable<Guid> ids);

    Task<IList<ProductPriceHistoryDto>> GetProductPriceHistoryAsync(Guid productId);
}