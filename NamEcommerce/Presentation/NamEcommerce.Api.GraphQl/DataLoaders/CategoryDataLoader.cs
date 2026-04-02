using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Api.GraphQl.DataLoaders;

public sealed class CategoryDataLoader : ICategoryDataLoader
{
    public const string GET_ALL = "CategoryDataLoader.GetAll";
    public const string GET_BY_ID = "CategoryDataLoader.GetById";

    private readonly ICategoryAppService _categoryAppService;

    public CategoryDataLoader(ICategoryAppService categoryAppService)
    {
        _categoryAppService = categoryAppService;
    }

    public async Task<IEnumerable<CategoryAppDto>> GetAllCategoriesAsync(CancellationToken cancellationToken)
    {
        var categoryData = await _categoryAppService.GetCategoriesAsync().ConfigureAwait(false);

        return categoryData;
    }

    public async Task<CategoryAppDto?> GetCategoryByIdAsync(CancellationToken cancellationToken, Guid? id)
    {
        if (!id.HasValue)
            return null;
        return await _categoryAppService.GetCategoryByIdAsync(id.Value).ConfigureAwait(false);
    }

    public async Task<IDictionary<Guid, CategoryAppDto>> GetCategoriesByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var categoryData = await _categoryAppService.GetCategoriesAsync().ConfigureAwait(false);

        return categoryData.Where(category => ids.Contains(category.Id))
            .ToDictionary(category => category.Id);
    }
}
