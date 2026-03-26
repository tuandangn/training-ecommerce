using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Services.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;

namespace NamEcommerce.Application.Services.Test.Catalog;

public sealed class CategoryAppServiceTests
{

    #region GetCategoriesAsync

    [Fact]
    public async Task GetCategoriesAsync_KeywordIsNotNull_ReturnsOrderedPagedData()
    {
        var keyword = "keyword";
        var pageIndex = 0;
        var pageSize = int.MaxValue;
        var pagedData = PagedDataDto.Create(
            [ 
                new CategoryDto(Guid.NewGuid()) { Name = $"{keyword}-1", DisplayOrder = 1, ParentId = null },
                new CategoryDto(Guid.NewGuid()) { Name = $"{keyword}-2", DisplayOrder = 2, ParentId = null }
            ],
            pageIndex, pageSize
        );
        var categoryManagerMock = CategoryManager.WhenGetCategoriesReturns(keyword, pageIndex, pageSize, pagedData);
        var categoryAppService = new CategoryAppService(categoryManagerMock.Object, null!);

        var pagedDataResult = await categoryAppService.GetCategoriesAsync(keyword, 0, int.MaxValue);

        Assert.Equal(pagedData.ElementAt(0).Id, pagedDataResult.Items.First().Id);
        Assert.Equal(pagedData.ElementAt(1).Id, pagedDataResult.Items.ElementAt(1).Id);
        Assert.Equal(2, pagedDataResult.Pagination.TotalCount);
        categoryManagerMock.Verify();
    }

    #endregion

    #region GetCategoryByIdAsync

    [Fact]
    public async Task GetCategoryByIdAsync_CategoryNotFound_ReturnsNull()
    {
        var notFoundId = Guid.NewGuid();
        var categoryDataReaderMock = CategoryDataReader.NotFound(notFoundId);
        var categoryAppService = new CategoryAppService(null!, categoryDataReaderMock.Object);

        var result = await categoryAppService.GetCategoryByIdAsync(notFoundId);

        Assert.Null(result);
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetCategoryByIdAsync_CategoryFound_ReturnsData()
    {
        var findId = Guid.NewGuid();
        var category = new Category(findId, "category");
        var categoryDataReaderMock = CategoryDataReader.CategoryById(category.Id, category);
        var categoryAppService = new CategoryAppService(null!, categoryDataReaderMock.Object);

        var result = await categoryAppService.GetCategoryByIdAsync(findId);

        Assert.Equal(category.Id, result!.Id);
        categoryDataReaderMock.Verify();
    }

    #endregion

    #region CreateCategoryAsync

    [Fact]
    public async Task CreateCategoryAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var categoryAppService = new CategoryAppService(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => categoryAppService.CreateCategoryAsync(null!));
    }

    [Fact]
    public async Task CreateCategoryAsync_DtoIsInvalid_ReturnsFalseResult()
    {
        var invalidDto = new CreateCategoryAppDto
        {
            Name = string.Empty,
            ParentId = null,
            DisplayOrder = 0
        };
        var categoryAppService = new CategoryAppService(null!, null!);
        var falseResult = await categoryAppService.CreateCategoryAsync(invalidDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
    }

    [Fact]
    public async Task CreateCategoryAsync_NameIsExists_ThrowsCategoryNameExistsException()
    {
        var existingName = "existing-name";
        var existingNameDto = new CreateCategoryAppDto
        {
            Name = existingName,
            ParentId = null,
            DisplayOrder = 0
        };
        var categoryManager = CategoryManager.SetUsernameExists(existingName, true);
        var categoryAppService = new CategoryAppService(categoryManager.Object, null!);

        var falseResult = await categoryAppService.CreateCategoryAsync(existingNameDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
        categoryManager.Verify();
    }

    [Fact]
    public async Task CreateCategoryAsync_ParentCategoryIsNotFound_ReturnFalseResult()
    {
        var notFoundParentId = Guid.NewGuid();
        var notFoundParentCategoryDto = new CreateCategoryAppDto
        {
            Name = "category",
            ParentId = notFoundParentId,
            DisplayOrder = 0
        };
        var categoryManagerStub = CategoryManager.SetUsernameExists(notFoundParentCategoryDto.Name, false);
        var categoryDataReaderMock = CategoryDataReader.NotFound(notFoundParentId);
        var categoryAppService = new CategoryAppService(categoryManagerStub.Object, categoryDataReaderMock.Object);

        var falseResult = await categoryAppService.CreateCategoryAsync(notFoundParentCategoryDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateCategoryAsync_NameIsNotExists_ReturnsResult()
    {
        var dto = new CreateCategoryAppDto
        {
            Name = "new-category",
            ParentId = null,
            DisplayOrder = 0
        };
        var createCategoryResult = new CreateCategoryResultDto
        {
            CreatedId = Guid.NewGuid()
        };
        var categoryManager = CategoryManager.SetUsernameExists(dto.Name, false)
            .CreateCategoryReturns(new CreateCategoryDto
            {
                Name = dto.Name,
                ParentId = null,
                DisplayOrder = dto.DisplayOrder
            }, createCategoryResult);
        var categoryAppService = new CategoryAppService(categoryManager.Object, null!);

        var result = await categoryAppService.CreateCategoryAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(createCategoryResult.CreatedId, result.CreatedId);
        categoryManager.Verify();
    }

    #endregion

    #region UpdateCategoryAsync

    [Fact]
    public async Task UpdateCategoryAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var categoryAppService = new CategoryAppService(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => categoryAppService.UpdateCategoryAsync(null!));
    }

    [Fact]
    public async Task UpdateCategoryAsync_DataIsInvalid_ReturnsFalseResult()
    {
        var invalidDto = new UpdateCategoryAppDto(Guid.NewGuid())
        {
            Name = string.Empty,
            ParentId = null,
            DisplayOrder = 0
        };
        var categoryAppService = new CategoryAppService(null!, null!);
        var falseResult = await categoryAppService.UpdateCategoryAsync(invalidDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
    }

    [Fact]
    public async Task UpdateCategoryAsync_CategoryIsNotFound_ReturnsFalseResult()
    {
        var notFoundCategoryId = Guid.NewGuid();
        var updateCategoryDto = new UpdateCategoryAppDto(notFoundCategoryId)
        {
            Name = "category",
            ParentId = null,
            DisplayOrder = 0
        };
        var categoryDataReaderMock = CategoryDataReader.NotFound(notFoundCategoryId);
        var categoryAppService = new CategoryAppService(null!, categoryDataReaderMock.Object);
        var falseResult = await categoryAppService.UpdateCategoryAsync(updateCategoryDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateCategoryAsync_NameIsExists_ThrowsReturnsFalseResult()
    {
        var existingName = "existing-name";
        var existingNameDto = new UpdateCategoryAppDto(Guid.NewGuid())
        {
            Name = existingName,
            ParentId = null,
            DisplayOrder = 0
        };
        var categoryManager = CategoryManager.SetUsernameExists(existingName, existingNameDto.Id, true);
        var categoryDataReaderStub = CategoryDataReader.CategoryById(
            existingNameDto.Id,
            new Category(existingNameDto.Id, "category")
        );
        var categoryAppService = new CategoryAppService(categoryManager.Object, categoryDataReaderStub.Object);

        var falseResult = await categoryAppService.UpdateCategoryAsync(existingNameDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
        categoryManager.Verify();
    }

    [Fact]
    public async Task UpdateCategoryAsync_ParentCategoryIsNotFound_ReturnFalseResult()
    {
        var notFoundCategoryId = Guid.NewGuid();
        var notFoundParentCategoryDto = new UpdateCategoryAppDto(Guid.NewGuid())
        {
            Name = "category",
            ParentId = notFoundCategoryId,
            DisplayOrder = 0
        };
        var categoryManagerStub = CategoryManager.SetUsernameExists(notFoundParentCategoryDto.Name, notFoundParentCategoryDto.Id, false);
        var categoryDataReaderMock = CategoryDataReader.CategoryById(
            notFoundParentCategoryDto.Id, new Category(notFoundParentCategoryDto.Id, "old-category")
        ).NotFound(notFoundParentCategoryDto.ParentId.Value);
        var categoryAppService = new CategoryAppService(categoryManagerStub.Object, categoryDataReaderMock.Object);

        var falseResult = await categoryAppService.UpdateCategoryAsync(notFoundParentCategoryDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateCategoryAsync_CauseCircularRelationship_ReturnsFalseResult()
    {
        var updatingCategoryId = Guid.NewGuid();
        var existingCategory = new Category(Guid.NewGuid(), "existing-category");
        var causeCircularRelationshipDto = new UpdateCategoryAppDto(updatingCategoryId)
        {
            Name = "new-category",
            ParentId = existingCategory.Id, //cause circular relationship
            DisplayOrder = 0
        };
        var categoryManagerStub = CategoryManager.SetUsernameExists(causeCircularRelationshipDto.Name, causeCircularRelationshipDto.Id, false);
        var categoryDataReaderMock = CategoryDataReader.CategoryById(
            causeCircularRelationshipDto.Id, new Category(causeCircularRelationshipDto.Id, "old-category")
        ).CategoryById(causeCircularRelationshipDto.ParentId.Value, existingCategory);
        await existingCategory.SetParentAsync(updatingCategoryId, categoryDataReaderMock.Object);
        var categoryAppService = new CategoryAppService(categoryManagerStub.Object, categoryDataReaderMock.Object);

        var falseResult = await categoryAppService.UpdateCategoryAsync(causeCircularRelationshipDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateCategoryAsync_NameIsNotExists_ReturnsResult()
    {
        var dto = new UpdateCategoryAppDto(Guid.NewGuid())
        {
            Name = "new-category",
            ParentId = null,
            DisplayOrder = 0
        };
        var updateCategoryResult = new UpdateCategoryResultDto(dto.Id)
        {
            Name = dto.Name,
            ParentId = dto.ParentId,
            DisplayOrder = dto.DisplayOrder
        };
        var categoryManager = CategoryManager.SetUsernameExists(dto.Name, updateCategoryResult.Id, false)
            .UpdateCategoryReturns(new UpdateCategoryDto(dto.Id)
            {
                Name = dto.Name,
                ParentId = dto.ParentId,
                DisplayOrder = dto.DisplayOrder
            }, updateCategoryResult);
        var categoryDataReaderStub = CategoryDataReader.CategoryById(dto.Id, new Category(dto.Id, "old-category"));
        var categoryAppService = new CategoryAppService(categoryManager.Object, categoryDataReaderStub.Object);

        var result = await categoryAppService.UpdateCategoryAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(updateCategoryResult.Id, result.UpdatedId);
        categoryManager.Verify();
    }

    #endregion

    #region DeleteCategoryAsync

    [Fact]
    public async Task DeleteCategoryAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var categoryAppService = new CategoryAppService(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => categoryAppService.DeleteCategoryAsync(null!));
    }

    [Fact]
    public async Task DeleteCategoryAsync_CategoryNotFound_ReturnsFalseResult()
    {
        var notFoundId = Guid.NewGuid();
        var notFoundDto = new DeleteCategoryAppDto(notFoundId);
        var categoryDataReaderMock = CategoryDataReader.NotFound(notFoundId);
        var categoryAppService = new CategoryAppService(null!, categoryDataReaderMock.Object);

        var falseResult = await categoryAppService.DeleteCategoryAsync(notFoundDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task DeleteCategoryAsync_CategoryFound_DeleteAndReturns()
    {
        var dto = new DeleteCategoryAppDto(Guid.NewGuid());
        var category = new Category(dto.Id, "category");
        var categoryDataReaderMock = CategoryDataReader.CategoryById(dto.Id, category);
        var categoryManagerMock = CategoryManager.CanDeleteCategory(dto.Id);
        var categoryAppService = new CategoryAppService(categoryManagerMock.Object, categoryDataReaderMock.Object);

        var result = await categoryAppService.DeleteCategoryAsync(dto);

        Assert.True(result.Success);
        categoryDataReaderMock.Verify();
        categoryManagerMock.Verify();
    }

    #endregion
}