using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Services.Catalog;

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

    public async Task<CreateCategoryResultAppDto> CreateCategoryAsync(CreateCategoryAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new CreateCategoryResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        if (await _categoryManager.DoesNameExistAsync(dto.Name).ConfigureAwait(false))
        {
            return new CreateCategoryResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.CategoryNameAlreadyExists"
            };
        }

        if (dto.ParentId.HasValue)
        {
            var parent = await _categoryDataReader.GetByIdAsync(dto.ParentId.Value).ConfigureAwait(false);
            if (parent is null)
            {
                return new CreateCategoryResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.CategoryIsNotFound"
                };
            }
        }

        var result = await _categoryManager.CreateCategoryAsync(new CreateCategoryDto
        {
            Name = dto.Name,
            DisplayOrder = dto.DisplayOrder,
            ParentId = dto.ParentId
        }).ConfigureAwait(false);

        return new CreateCategoryResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
        };
    }

    public async Task<DeleteCategoryResultAppDto> DeleteCategoryAsync(DeleteCategoryAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var category = await _categoryDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (category == null)
        {
            return new DeleteCategoryResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.CategoryIsNotFound"
            };
        }

        await _categoryManager.DeleteCategoryAsync(dto.Id).ConfigureAwait(false);

        return new DeleteCategoryResultAppDto { Success = true };
    }

    public async Task<CategoryAppDto?> GetCategoryByIdAsync(Guid id)
    {
        var category = await _categoryDataReader.GetByIdAsync(id).ConfigureAwait(false);
        return category?.ToDto();
    }

    public async Task<IPagedDataAppDto<CategoryAppDto>> GetCategoriesAsync(string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var pagedData = await _categoryManager.GetCategoriesAsync(keywords, pageIndex, pageSize).ConfigureAwait(false);

        var result = PagedDataAppDto.Create(
            pagedData.Select(category => category.ToDto()),
            pageIndex, pageSize, pagedData.PagerInfo.TotalCount);

        return result;
    }

    public async Task<IEnumerable<CategoryAppDto>> GetCategoryBreadcrumbAsync(Guid categoryId)
    {
        var allCategories = await _categoryDataReader.GetAllAsync().ConfigureAwait(false);

        var breadcrumbItems = new List<Category>();
        populateBreadcrumbItems(categoryId);
        breadcrumbItems.Reverse();

        return breadcrumbItems.Select(category => category.ToDto());

        //local method
        void populateBreadcrumbItems(Guid categoryId)
        {
            var category = allCategories.FirstOrDefault(category => category.Id == categoryId);
            if (category == null)
                return;
            breadcrumbItems.Add(category);
            if (!category.ParentId.HasValue)
                return;
            populateBreadcrumbItems(category.ParentId.Value);
        }
    }

    public async Task<UpdateCategoryResultAppDto> UpdateCategoryAsync(UpdateCategoryAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new UpdateCategoryResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var category = await _categoryDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (category == null)
        {
            return new UpdateCategoryResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.CategoryIsNotFound"
            };
        }

        if (await _categoryManager.DoesNameExistAsync(dto.Name, dto.Id).ConfigureAwait(false))
        {
            return new UpdateCategoryResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.CategoryNameAlreadyExists"
            };
        }

        if (dto.ParentId.HasValue)
        {
            var parent = await _categoryDataReader.GetByIdAsync(dto.ParentId.Value);
            if (parent is null)
            {
                return new UpdateCategoryResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.CategoryIsNotFound"
                };
            }
            if (parent.ParentId == dto.Id)
            {
                return new UpdateCategoryResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.CategoryCircularRelationship"
                };
            }
        }

        var result = await _categoryManager.UpdateCategoryAsync(new UpdateCategoryDto(dto.Id)
        {
            Name = dto.Name,
            DisplayOrder = dto.DisplayOrder,
            ParentId = dto.ParentId
        }).ConfigureAwait(false);

        return new UpdateCategoryResultAppDto
        {
            Success = true,
            UpdatedId = result.Id
        };
    }
}