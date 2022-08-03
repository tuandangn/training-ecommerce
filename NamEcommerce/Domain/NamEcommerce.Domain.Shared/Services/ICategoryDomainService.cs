using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Shared.Services;

public interface ICategoryDomainService
{
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);

    Task<CategoryDto> UpdateCategoryAsync(UpdateCategoryDto dto);

    Task DeleteCategoryAsync(Guid categoryId);

    Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null);

    Task<CategoryDto> SetParentCategory(Guid categoryId, Guid parentId, int onParentDisplayOrder);
}
