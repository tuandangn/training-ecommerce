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

    Task<CreateProductResultDto> CreateProductAsync(CreateProductDto dto);

    Task<UpdateProductResultDto> UpdateProductAsync(UpdateProductDto dto);

    Task RemoveProductFromCategoryAsync(Guid productId, Guid categoryId);

    Task DeleteProductAsync(Guid id);
    Task<IEnumerable<ProductPriceHistoryDto>> GetProductPriceHistoryAsync(Guid productId);
}
