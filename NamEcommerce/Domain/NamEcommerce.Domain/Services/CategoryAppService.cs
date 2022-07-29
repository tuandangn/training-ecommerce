using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;

namespace NamEcommerce.Domain.Services;

public sealed class CategoryAppService : ICategoryAppService
{
    private readonly IRepository<Category> _categoryRepository;

    public CategoryAppService(IRepository<Category> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));
        if (await DoesNameExistAsync(dto.Name, null).ConfigureAwait(false))
            throw new CategoryNameExistsException(dto.Name);

        var insertedCategory = await _categoryRepository.InsertAsync(
            new Category(default, dto.Name)
            {
                DisplayOrder = dto.DisplayOrder
            }).ConfigureAwait(false);
        return new CategoryDto(insertedCategory.Id, insertedCategory.Name, insertedCategory.DisplayOrder);
    }

    public async Task<bool> DoesNameExistAsync(string name, int? id = null)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        var categories = await _categoryRepository.GetAllAsync().ConfigureAwait(false);

        return categories.Any(category => string.Equals(category.Name, name));
    }
}
