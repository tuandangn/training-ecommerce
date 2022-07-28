using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Services;

public interface ICategoryAppService
{
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);

    Task<bool> DoesNameExistAsync(string name, int? id = null);
}
