using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Catalog;

namespace NamEcommerce.Domain.Services.Catalog;

public sealed class CategoryManager : ICategoryManager
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly IEntityDataReader<Category> _categoryDataReader;
    private readonly IEventPublisher _eventPublisher;

    public CategoryManager(IRepository<Category> categoryRepository, IEntityDataReader<Category> categoryDataReader, IEventPublisher eventPublisher)
    {
        _categoryRepository = categoryRepository;
        _categoryDataReader = categoryDataReader;
        _eventPublisher = eventPublisher;
    }

    public async Task<CreateCategoryResultDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (await DoesNameExistAsync(dto.Name, null).ConfigureAwait(false))
            throw new CategoryNameExistsException(dto.Name);

        var category = new Category(Guid.NewGuid(), dto.Name)
        {
            DisplayOrder = dto.DisplayOrder
        };
        await category.SetParentAsync(dto.ParentId, _categoryDataReader).ConfigureAwait(false);
        var insertedCategory = await _categoryRepository.InsertAsync(category).ConfigureAwait(false);

        await _eventPublisher.EntityCreated(insertedCategory).ConfigureAwait(false);

        return new CreateCategoryResultDto
        {
            CreatedId = insertedCategory.Id
        };
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        var category = await _categoryDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (category is null)
            throw new CategoryIsNotFoundException(id);

        await _categoryRepository.DeleteAsync(category).ConfigureAwait(false);

        var children = (await _categoryDataReader.GetAllAsync().ConfigureAwait(false))
            .Where(cat => cat.ParentId == category.Id).ToList();
        foreach (var child in children)
        {
            child.RemoveParent();
            await _categoryRepository.UpdateAsync(child).ConfigureAwait(false);
        }

        await _eventPublisher.EntityDeleted(category).ConfigureAwait(false);
    }

    public Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var query = from category in _categoryDataReader.DataSource
                    where category.Name == name && (comparesWithCurrentId == null || category.Id != comparesWithCurrentId)
                    select category;

        var sameNameExists = query.FirstOrDefault() != null;
        return Task.FromResult(sameNameExists);
    }

    public Task<IPagedDataDto<CategoryDto>> GetCategoriesAsync(string? keywords, int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = _categoryDataReader.DataSource;

        if (!string.IsNullOrEmpty(keywords))
        {
            var normizedKeywords = TextHelper.Normalize(keywords);
            query = query.Where(c => c.NormalizedName.Contains(normizedKeywords));
        }

        query = query.OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name);

        var totalCount = query.Count();
        var pagedData = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var data = PagedDataDto.Create(pagedData.Select(category => category.ToDto()), pageIndex, pageSize, totalCount);
        return Task.FromResult(data);
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id)
    {
        var category = await _categoryDataReader.GetByIdAsync(id);
        if (category is null)
            return null;

        return category.ToDto();
    }

    public async Task<CategoryDto> SetParentCategoryAsync(Guid categoryId, Guid parentId, int onParentDisplayOrder)
    {
        var child = await _categoryDataReader.GetByIdAsync(categoryId).ConfigureAwait(false);
        if (child is null)
            throw new CategoryIsNotFoundException(categoryId);

        await child.SetParentAsync(parentId, _categoryDataReader).ConfigureAwait(false);
        var updatedCategory = await _categoryRepository.UpdateAsync(child).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedCategory, null).ConfigureAwait(false);

        return updatedCategory.ToDto();
    }

    public async Task<UpdateCategoryResultDto> UpdateCategoryAsync(UpdateCategoryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var category = await _categoryDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (category is null)
            throw new CategoryIsNotFoundException(dto.Id);

        if (await DoesNameExistAsync(dto.Name, dto.Id).ConfigureAwait(false))
            throw new CategoryNameExistsException(dto.Name);

        await category.SetNameAsync(dto.Name, this).ConfigureAwait(false);
        category.DisplayOrder = dto.DisplayOrder;
        if (dto.ParentId.HasValue)
            await category.SetParentAsync(dto.ParentId.Value, _categoryDataReader).ConfigureAwait(false);
        else
            category.RemoveParent();

        var updatedCategory = await _categoryRepository.UpdateAsync(category).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedCategory).ConfigureAwait(false);

        return new UpdateCategoryResultDto(updatedCategory.Id)
        {
            Name = dto.Name,
            ParentId = dto.ParentId,
            DisplayOrder = dto.DisplayOrder
        };
    }
}
