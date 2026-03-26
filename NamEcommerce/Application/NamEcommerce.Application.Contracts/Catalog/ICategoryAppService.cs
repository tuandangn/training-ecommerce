using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Catalog;

public interface ICategoryAppService
{
    Task<IPagedDataAppDto<CategoryAppDto>> GetCategoriesAsync(string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<CategoryAppDto?> GetCategoryByIdAsync(Guid id);

    Task<CreateCategoryResultAppDto> CreateCategoryAsync(CreateCategoryAppDto dto);

    Task<UpdateCategoryResultAppDto> UpdateCategoryAsync(UpdateCategoryAppDto dto);

    Task<DeleteCategoryResultAppDto> DeleteCategoryAsync(DeleteCategoryAppDto dto);

    Task<IEnumerable<CategoryAppDto>> GetCategoryBreadcrumbAsync(Guid categoryId);
}
