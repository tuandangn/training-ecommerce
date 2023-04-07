using MediatR;
using NamEcommerce.Api.GraphQl.DataLoaders;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;

namespace NamEcommerce.Api.GraphQl.Test;

public sealed class CategoryDataLoaderTests
{
    #region GetAllCategoriesAsync

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsResult()
    {
        var categories = new[] { new CategoryDto(Guid.NewGuid(), "Category 1") };
        var query = new GetAllCategories();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(mediator => mediator.Send(query, default)).ReturnsAsync(categories);
        var categoryDataLoader = new CategoryDataLoader(mediatorMock.Object);

        var result = await categoryDataLoader.GetAllCategoriesAsync(default);

        Assert.Equal(categories.Length, result.Count());
        Assert.Equal(categories, result);
        mediatorMock.Verify();
    }

    #endregion

    #region GetCategoryByIdAsync

    [Fact]
    public async Task GetCategoryByIdAsync_ReturnsResult()
    {
        var category = new CategoryDto(Guid.NewGuid(), "Category 1");
        var query = new GetCategoryById(category.Id);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(mediator => mediator.Send(query, default)).ReturnsAsync(category);
        var categoryDataLoader = new CategoryDataLoader(mediatorMock.Object);

        var result = await categoryDataLoader.GetCategoryByIdAsync(default, category.Id);

        Assert.Equal(category, result);
        mediatorMock.Verify();
    }

    #endregion

    #region GetCategoriesByIdsAsync

    [Fact]
    public async Task GetCategoriesByIdsAsync_ReturnsResult()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var categories = new[] { new CategoryDto(ids[0], "Category 1"), new CategoryDto(ids[1], "Category 2") };
        var query = new GetCategoriesByIds(ids);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(mediator => mediator.Send(query, default)).ReturnsAsync(categories);
        var categoryDataLoader = new CategoryDataLoader(mediatorMock.Object);

        var result = await categoryDataLoader.GetCategoriesByIdsAsync(ids, default);

        Assert.Equal(2, result.Count);
        Assert.Equal(categories[0], result[categories[0].Id]);
        Assert.Equal(categories[1], result[categories[1].Id]);
        mediatorMock.Verify();
    }

    #endregion
}
