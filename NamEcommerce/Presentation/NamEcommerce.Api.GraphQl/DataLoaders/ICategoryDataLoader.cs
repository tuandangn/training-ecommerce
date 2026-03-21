using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Api.GraphQl.DataLoaders;

public interface ICategoryDataLoader
{
    Task<IEnumerable<CategoryAppDto>> GetAllCategoriesAsync(CancellationToken cancellationToken);
    Task<IDictionary<Guid, CategoryAppDto>> GetCategoriesByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
    Task<CategoryAppDto?> GetCategoryByIdAsync(CancellationToken cancellationToken, Guid? id);
}