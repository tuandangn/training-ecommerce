using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Services;

public interface ICategoryDomainService
{
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);

    Task<CategoryDto> UpdateCategoryAsync(UpdateCategoryDto dto);

    Task DeleteCategoryAsync(int categoryId);

    Task<bool> DoesNameExistAsync(string name, int? comparesWithCurrentId = null);

    Task<CategoryDto> SetParentCategory(int categoryId, int parentId, int onParentDisplayOrder);
}
