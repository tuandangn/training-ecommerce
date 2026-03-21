using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Catalog;

public interface ICategoryAppService
{
    Task<IPagedDataDto<CategoryAppDto>> GetCategoriesAsync(int pageIndex = 0, int pageSize = int.MaxValue);

    Task<IEnumerable<CategoryAppDto>> GetCategoriesByIdsAsync(IEnumerable<Guid> ids);

    Task<CategoryAppDto?> GetCategoryByIdAsync(Guid id);
}
