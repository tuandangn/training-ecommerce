using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Catalog;

public sealed class CategoryAppService : ICategoryAppService
{
    private readonly ICategoryManager _categoryManager;
    private readonly IEntityDataReader<Category> _categoryDataReader;

    public CategoryAppService(ICategoryManager categoryManager, IEntityDataReader<Category> categoryDataReader)
    {
        _categoryManager = categoryManager;
        _categoryDataReader = categoryDataReader;
    }

    public async Task<IPagedDataDto<CategoryAppDto>> GetCategoriesAsync(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var orderedQuery = _categoryDataReader.DataSource
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name);
        var totalCount = orderedQuery.Count();
        var pagedData = orderedQuery
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();
        return PagedDataDto.Create(pagedData.Select(category => category.ToDto()), pageIndex, pageSize, totalCount);
    }

    public async Task<IEnumerable<CategoryAppDto>> GetCategoriesByIdsAsync(IEnumerable<Guid> ids)
    {
        if (ids is null)
            throw new ArgumentNullException(nameof(ids));

        if (!ids.Any())
            return Enumerable.Empty<CategoryAppDto>();

        var query = from category in _categoryDataReader.DataSource
                    where ids.Contains(category.Id)
                    select category;
        var categories = query.ToList();

        return categories.Select(category => category.ToDto());
    }

    public async Task<CategoryAppDto?> GetCategoryByIdAsync(Guid id)
    {
        var category = await _categoryDataReader.GetByIdAsync(id);
        return category?.ToDto();
    }
}
