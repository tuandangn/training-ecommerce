using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Domain.Shared.Services.Catalog;

public interface ICategoryManager
{
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id);

    Task<IPagedDataDto<CategoryDto>> GetCategoriesAsync(string? keywords, int pageIndex, int pageSize);

    Task<CreateCategoryResultDto> CreateCategoryAsync(CreateCategoryDto dto);

    Task<UpdateCategoryResultDto> UpdateCategoryAsync(UpdateCategoryDto dto);

    Task DeleteCategoryAsync(Guid id);

    Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null);

    Task<CategoryDto> SetParentCategoryAsync(Guid categoryId, Guid parentId, int onParentDisplayOrder);
}
