using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Domain.Shared.Services.Catalog;

public interface IProductManager
{
    Task<IPagedDataDto<ProductDto>> GetProductsAsync(string? keywords, int pageIndex, int pageSize);

    Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null);

    Task<CreateProductResultDto> CreateProductAsync(CreateProductDto dto);

    Task<UpdateProductResultDto> UpdateProductAsync(UpdateProductDto dto);

    Task DeleteProductAsync(Guid id);
}
