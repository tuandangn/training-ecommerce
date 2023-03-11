using NamEcommerce.Application.Services.Queries.Handlers.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;

namespace NamEcommerce.Application.Services.Test.Queries.Handlers;

public sealed class GetAllCategoriesHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllCategories()
    {
        var firstCategory = new Domain.Shared.Dtos.Catalog.CategoryDto(Guid.NewGuid(), "Category 1") { DisplayOrder = 1 };
        var secondCategory = new Domain.Shared.Dtos.Catalog.CategoryDto(Guid.NewGuid(), "Category 2") { DisplayOrder = 2 };
        var unorderedCategories = new[] { secondCategory, firstCategory };
        var categoryManagerMock = CategoryManager.GetAllCategoriesWillReturns(unorderedCategories);

        var categoryAppService = new GetAllCategoriesHandler(categoryManagerMock.Object);
        var allCategories = await categoryAppService.Handle(default, default);

        Assert.Collection(allCategories,
            cat1 => Assert.Equal(firstCategory.Id, cat1.Id),
            cat2 => Assert.Equal(secondCategory.Id, cat2.Id));
        categoryManagerMock.Verify();
    }
}
