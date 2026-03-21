using NamEcommerce.Application.Services.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Domain.Entities.Catalog;

namespace NamEcommerce.Application.Services.Test.Catalog;

public sealed class CategoryAppServiceTests
{

    #region GetCategoriesAsync

    [Fact]
    public async Task GetCategoriesAsync_DataIsEmpty_ReturnsEmpty()
    {
        var categoryDataReaderMock = CategoryDataReader.SetData([]);
        var categoryAppService = new CategoryAppService(null!, categoryDataReaderMock.Object);

        var pagedDataResult = await categoryAppService.GetCategoriesAsync(1, 1);

        Assert.Equal(0, pagedDataResult.Pagination.TotalCount);
        categoryDataReaderMock.Verify(m => m.DataSource, Times.Once);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsOrderedPagedData()
    {
        var category1 = new Category(Guid.NewGuid(), "category-1")
        {
            DisplayOrder = 2
        };
        var category2 = new Category(Guid.NewGuid(), "category-2")
        {
            DisplayOrder = 1
        };
        var category3 = new Category(Guid.NewGuid(), "category-3")
        {
            DisplayOrder = 1
        };
        var categoryDataReaderMock = CategoryDataReader.SetData(
            category1, category2, category3
        );

        var categoryAppService = new CategoryAppService(null!, categoryDataReaderMock.Object);

        var pagedDataResult = await categoryAppService.GetCategoriesAsync(1, 1);

        Assert.Equal(3, pagedDataResult.Pagination.TotalCount);
        Assert.Equal(category3.Id, pagedDataResult.Items.First().Id);
        categoryDataReaderMock.Verify();
    }

    #endregion

    #region GetCategoriesAsync

    [Fact]
    public async Task GetCategoriesByIdsAsync_IdsIsNull_ThrowsArgumentNullException()
    {
        var categoryAppService = new CategoryAppService(null!, null!);
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => categoryAppService.GetCategoriesByIdsAsync(null!));
    }

    [Fact]
    public async Task GetCategoriesByIdsAsync_IdsIsEmpty_ReturnsEmpty()
    {
        var categoryAppService = new CategoryAppService(null!, null!);
        var result = await categoryAppService.GetCategoriesByIdsAsync([]);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCategoriesByIdsAsync_IdsIsNotEmpty_ReturnsFoundCategories()
    {
        var category1 = new Category(Guid.NewGuid(), "category-1");
        var category2 = new Category(Guid.NewGuid(), "category-2");
        var category3 = new Category(Guid.NewGuid(), "category-3");
        var categoryDataReaderMock = CategoryDataReader.SetData(
            category1, category2, category3
        );
        var categoryAppService = new CategoryAppService(null!, categoryDataReaderMock.Object);
        var result = await categoryAppService.GetCategoriesByIdsAsync([category1.Id, category2.Id]);

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(category1.Id, result.First().Id);
        Assert.Equal(category2.Id, result.ElementAt(1).Id);
        categoryDataReaderMock.Verify();
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
}
