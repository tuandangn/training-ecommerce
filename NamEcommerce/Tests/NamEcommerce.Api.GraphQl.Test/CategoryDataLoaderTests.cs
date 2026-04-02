using MediatR;
using NamEcommerce.Api.GraphQl.DataLoaders;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Api.GraphQl.Test;

public sealed class CategoryDataLoaderTests
{
    #region GetAllCategoriesAsync

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsResult()
    {
        var categories = new[] {
            new CategoryAppDto(Guid.NewGuid())
            {
                Name = "category",
                ParentId = null
            }
        };
        var categoryAppServiceMock = new Mock<ICategoryAppService>();
        categoryAppServiceMock.Setup(service => service.GetCategoriesAsync()).ReturnsAsync(PagedDataAppDto.Create(categories));
        var categoryDataLoader = new CategoryDataLoader(categoryAppServiceMock.Object);

        var result = await categoryDataLoader.GetAllCategoriesAsync(default);

        Assert.Equal(categories.Length, result.Count());
        Assert.Equal(categories, result);
        categoryAppServiceMock.Verify();
    }

    #endregion

    #region GetCategoryByIdAsync

    [Fact]
    public async Task GetCategoryByIdAsync_ReturnsResult()
    {
        var category = new CategoryAppDto(Guid.NewGuid())
        {
            Name = "category",
            ParentId = null
        };
        var categoryAppServiceMock = new Mock<ICategoryAppService>();
        categoryAppServiceMock.Setup(service => service.GetCategoryByIdAsync(category.Id)).ReturnsAsync(category);
        var categoryDataLoader = new CategoryDataLoader(categoryAppServiceMock.Object);

        var result = await categoryDataLoader.GetCategoryByIdAsync(default, category.Id);

        Assert.Equal(category, result);
        categoryAppServiceMock.Verify();
    }

    #endregion

    #region GetCategoriesByIdsAsync

    [Fact]
    public async Task GetCategoriesByIdsAsync_ReturnsResult()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var categories = new[] {
            new CategoryAppDto(ids[0])
            {
                Name = "Category 1",
                ParentId = null
            },
            new CategoryAppDto(ids[1])
            {
                Name = "Category 2",
                ParentId = null
            }
        };
        var categoryAppServiceMock = new Mock<ICategoryAppService>();
        categoryAppServiceMock.Setup(service => service.GetCategoriesAsync()).ReturnsAsync(PagedDataAppDto.Create(categories));
        var categoryDataLoader = new CategoryDataLoader(categoryAppServiceMock.Object);

        var result = await categoryDataLoader.GetCategoriesByIdsAsync(ids, default);

        Assert.Equal(2, result.Count);
        Assert.Equal(categories[0], result[categories[0].Id]);
        Assert.Equal(categories[1], result[categories[1].Id]);
        categoryAppServiceMock.Verify();
    }

    #endregion
}
