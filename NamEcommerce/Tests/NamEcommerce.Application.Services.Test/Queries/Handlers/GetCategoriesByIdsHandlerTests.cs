using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Application.Services.Queries.Handlers.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;
using NamEcommerce.Domain.Entities.Catalog;

namespace NamEcommerce.Application.Services.Test.Queries.Handlers;

public sealed class GetCategoriesByIdsHandlerTests
{
    [Fact]
    public async Task Handle_IdsIsEmpty_ReturnEmpty()
    {
        var request = new GetCategoriesByIds(Enumerable.Empty<Guid>());
        var handler = new GetCategoriesByIdsHandler(null!);

        var emptyResult = await handler.Handle(request, default);

        Assert.Empty(emptyResult);
    }

    [Fact]
    public async Task Handle_ReturnResult()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var categories = new[]
        {
            new Category(ids[0], "Category 1"),
            new Category(ids[1], "Category 2")
        };
        var categoryDataReaderMock = CategoryDataReader.CategoriesByIds(
            ids, categories
        );
        var request = new GetCategoriesByIds(ids);
        var handler = new GetCategoriesByIdsHandler(categoryDataReaderMock.Object);

        var result = await handler.Handle(request, default);

        Assert.Equal(categories[0].ToDto(), result.ElementAt(0));
        Assert.Equal(categories[1].ToDto(), result.ElementAt(1));
        categoryDataReaderMock.Verify();
    }
}
