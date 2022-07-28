namespace NamEcommerce.Domain.Test.Services;

public sealed class CategoryAppServiceTests
{
    #region CreateCategoryAsync

    [Fact]
    public async Task CreateCategoryAsync_InputDtoIsNull_ThrowsArgumentNullException()
    {
        var categoryAppService = new CategoryAppService(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            categoryAppService.CreateCategoryAsync(null!)
        );
    }

    [Fact]
    public async Task CreateCategoryAsync_NameIsExists_ThrowsCategoryNameExistsException()
    {
        var testName = "test-name-existing";
        var categoryRepositoryMock = CategoryRepository.SetNameExists(testName);

        var categoryAppService = new CategoryAppService(categoryRepositoryMock.Object);

        await Assert.ThrowsAsync<CategoryNameExistsException>(() =>
            categoryAppService.CreateCategoryAsync(new CreateCategoryDto(testName, default))
        );
        categoryRepositoryMock.Verify();
    }

    [Fact]
    public async Task CreateCategoryAsync_DtoIsValid_ReturnsCreatedCategoryDto()
    {
        var category = new Category(0, "name") { DisplayOrder = 1 };
        var returnCategory = category with
        {
            Id = 1
        };
        var categoryRepositoryMock = CategoryRepository.Create(category, returnCategory);
        var categoryAppService = new CategoryAppService(categoryRepositoryMock.Object);

        var categoryDto = await categoryAppService.CreateCategoryAsync(new CreateCategoryDto(category.Name, category.DisplayOrder));

        Assert.Equal(
            (categoryDto.Id, category.Name, category.DisplayOrder),
            (returnCategory.Id, returnCategory.Name, returnCategory.DisplayOrder));
        categoryRepositoryMock.Verify();
    }

    #endregion

    #region DoesNameExistAsync

    [Fact]
    public async Task DoesNameExistAsync_NameIsNull_ThrowsArgumentNullException()
    {
        var categoryAppService = new CategoryAppService(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            categoryAppService.DoesNameExistAsync(null!)
        );
    }

    [Fact]
    public async Task DoesNameExistAsync_NameIsMatch_ReturnsTrue()
    {
        var testName = "test-name-existing";
        var categoryRepositoryMock = CategoryRepository.SetNameExists(testName);
        var categoryAppService = new CategoryAppService(categoryRepositoryMock.Object);

        var nameExists = await categoryAppService.DoesNameExistAsync(testName);

        Assert.True(nameExists);
        categoryRepositoryMock.Verify();
    }

    #endregion
}
