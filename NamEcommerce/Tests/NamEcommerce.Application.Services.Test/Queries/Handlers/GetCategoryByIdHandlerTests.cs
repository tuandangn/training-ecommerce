using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Application.Services.Queries.Handlers.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;
using NamEcommerce.Domain.Entities.Catalog;

namespace NamEcommerce.Application.Services.Test.Queries.Handlers;

public sealed class GetCategoryByIdHandlerTests
{
    [Fact]
    public async Task Handle_NotFoundCategory_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var categoryManagerMock = CategoryDataReader.NotFound(id);

        var getCategoryByIdHandler = new GetCategoryByIdHandler(categoryManagerMock.Object);
        var category = await getCategoryByIdHandler.Handle(new GetCategoryById(id), default);

        Assert.Null(category);
        categoryManagerMock.Verify();
    }

    [Fact]
    public async Task Handle_FoundCategory_ReturnsCategoryDto()
    {
        var id = Guid.NewGuid();
        var category = new Category(id, "Category name")
        {
            DisplayOrder = 12
        };
        var categoryManagerMock = CategoryDataReader.CategoryById(id, category);

        var getCategoryByIdHandler = new GetCategoryByIdHandler(categoryManagerMock.Object);
        var foundCategory = await getCategoryByIdHandler.Handle(new GetCategoryById(id), default);

        Assert.Equal(category.ToDto(), foundCategory);
        categoryManagerMock.Verify();
    }
}
