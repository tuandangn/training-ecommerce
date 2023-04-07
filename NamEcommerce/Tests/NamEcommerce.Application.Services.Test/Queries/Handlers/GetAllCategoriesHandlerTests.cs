using NamEcommerce.Application.Services.Queries.Handlers.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Domain.Entities.Catalog;

namespace NamEcommerce.Application.Services.Test.Queries.Handlers;

public sealed class GetAllCategoriesHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllCategories()
    {
        var firstCategory = new Category(Guid.NewGuid(), "Category 1") { DisplayOrder = 1 };
        var secondCategory = new Category(Guid.NewGuid(), "Category 2") { DisplayOrder = 2 };
        var unorderedCategories = new[] { secondCategory, firstCategory };
        var categoryManagerMock = CategoryDataReader.AllCategories(unorderedCategories);

        var getAllCategoriesHandler = new GetAllCategoriesHandler(categoryManagerMock.Object);
        var allCategories = await getAllCategoriesHandler.Handle(default!, default);

        Assert.Equal(2, allCategories.Count());
        Assert.Collection(allCategories,
            cat1 => Assert.Equal(firstCategory.Id, cat1.Id),
            cat2 => Assert.Equal(secondCategory.Id, cat2.Id));
        categoryManagerMock.Verify();
    }
}
