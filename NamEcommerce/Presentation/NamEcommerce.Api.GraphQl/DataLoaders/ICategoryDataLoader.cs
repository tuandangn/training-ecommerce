using NamEcommerce.Application.Shared.Dtos.Catalog;

namespace NamEcommerce.Api.GraphQl.DataLoaders;

public interface ICategoryDataLoader
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken);
    Task<IDictionary<Guid, CategoryDto>> GetCategoriesByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
    Task<CategoryDto?> GetCategoryByIdAsync(CancellationToken cancellationToken, Guid? id);
}