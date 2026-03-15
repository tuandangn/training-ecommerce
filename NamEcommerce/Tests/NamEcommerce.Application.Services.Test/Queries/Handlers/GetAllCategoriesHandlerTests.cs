using NamEcommerce.Application.Services.Queries.Handlers.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Domain.Entities.Catalog;

namespace NamEcommerce.Application.Services.Test.Queries.Handlers;

public sealed class GetAllCategoriesHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllCategories()
    {
        var firstCategory = new Category(Guid.NewGuid(), "Category 1") { DisplayOrder = 2 }; // ordered is last
        var secondCategory = new Category(Guid.NewGuid(), "Category 2") { DisplayOrder = 1 }; // ordered is second
        var thirdCategory = new Category(Guid.NewGuid(), "Category") { DisplayOrder = 1 }; // ordered is first
        var unorderedCategories = new[] { secondCategory, firstCategory, thirdCategory };
        var categoryManagerMock = CategoryDataReader.AllCategories(unorderedCategories);

        var getAllCategoriesHandler = new GetAllCategoriesHandler(categoryManagerMock.Object);
        var allCategories = await getAllCategoriesHandler.Handle(default!, default);

        Assert.Equal(3, allCategories.Count());
        Assert.Collection(allCategories,
            cat => Assert.Equal(thirdCategory.Id, cat.Id),
            cat => Assert.Equal(secondCategory.Id, cat.Id),
            cat => Assert.Equal(firstCategory.Id, cat.Id)
        );
        categoryManagerMock.Verify();
    }
}
