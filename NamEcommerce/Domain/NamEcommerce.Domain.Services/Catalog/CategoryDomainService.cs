using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Services.Catalog;

public sealed class CategoryDomainService : ICategoryDomainService
{
    private readonly IRepository<Category> _categoryRepository;

    public CategoryDomainService(IRepository<Category> categoryRepository)
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
        return insertedCategory.ToDto();
    }

    public async Task DeleteCategoryAsync(Guid categoryId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId).ConfigureAwait(false);
        if (category is null)
            throw new ArgumentException("Category is not found", nameof(categoryId));

        await _categoryRepository.DeleteAsync(category);

        var children = (await _categoryRepository.GetAllAsync().ConfigureAwait(false))
            .Where(cat => cat.ParentId == category.Id).ToList();
        foreach(var child in children)
        {
            await _categoryRepository.UpdateAsync(child with
            {
                ParentId = null
            });
        }
    }

    public async Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        var categories = await _categoryRepository.GetAllAsync().ConfigureAwait(false);

        return categories.Any(category =>
            string.Equals(category.Name, name)
            && (!comparesWithCurrentId.HasValue || category.Id != comparesWithCurrentId));
    }

    public async Task<CategoryDto> SetParentCategory(Guid categoryId, Guid parentId, int onParentDisplayOrder)
    {
        var child = await _categoryRepository.GetByIdAsync(categoryId).ConfigureAwait(false);
        if (child is null)
            throw new ArgumentException("Category is not found", nameof(categoryId));

        var parent = await _categoryRepository.GetByIdAsync(parentId).ConfigureAwait(false);
        if (parent is null)
            throw new ArgumentException("Category is not found", nameof(parentId));

        if (parent.ParentId == child.Id)
            throw new CategoryCircularRelationshipException(child.Name, parent.Name);

        var result = await _categoryRepository.UpdateAsync(child with
        {
            ParentId = parent.Id,
            OnParentDisplayOrder = onParentDisplayOrder
        }).ConfigureAwait(false);

        return result.ToDto();
    }

    public async Task<CategoryDto> UpdateCategoryAsync(UpdateCategoryDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        var category = await _categoryRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (category is null)
            throw new ArgumentException("Category is not found", nameof(dto.Id));

        if (await DoesNameExistAsync(dto.Name, dto.Id).ConfigureAwait(false))
            throw new CategoryNameExistsException(dto.Name);

        var result = await _categoryRepository.UpdateAsync(category with
        {
            Name = dto.Name,
            DisplayOrder = dto.DisplayOrder
        }).ConfigureAwait(false);

        return result.ToDto();
    }
}
