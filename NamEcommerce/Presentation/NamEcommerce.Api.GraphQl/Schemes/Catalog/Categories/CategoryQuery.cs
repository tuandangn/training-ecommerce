using GraphQL.Types;
using MediatR;
using NamEcommerce.Api.GraphQl.Models.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;

namespace NamEcommerce.Api.GraphQl.Schemes.Catalog.Categories;

public sealed class CategoryQuery : ObjectGraphType
{
    public CategoryQuery()
    {
        Name = "CategoryQuery";
        Description = "Describes category queries";

        Field<ListGraphType<CategoryType>>("all").ResolveAsync(async context =>
        {
            var mediator = context.RequestServices?.GetRequiredService<IMediator>();
            if (mediator is null)
                throw new InvalidOperationException("Cannot retrieve category data");

            var categories = await mediator.Send(new GetAllCategories(), context.CancellationToken);
            return categories.Select(category => new CategoryModel(category.Id, category.Name)).ToList();
        });
    }
}
