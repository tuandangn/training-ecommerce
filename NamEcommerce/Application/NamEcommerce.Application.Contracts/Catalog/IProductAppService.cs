using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Catalog;

public interface IProductAppService
{
    Task<IPagedDataAppDto<ProductAppDto>> GetProductsAsync(string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<ProductAppDto?> GetProductByIdAsync(Guid id);

    Task<CreateProductResultAppDto> CreateProductAsync(CreateProductAppDto dto);

    Task<UpdateProductResultAppDto> UpdateProductAsync(UpdateProductAppDto dto);

    Task<DeleteProductResultAppDto> DeleteProductAsync(DeleteProductAppDto dto);
}
