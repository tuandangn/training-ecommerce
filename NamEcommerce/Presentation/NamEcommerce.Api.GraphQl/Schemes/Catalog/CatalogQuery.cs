using GraphQL.Types;
using NamEcommerce.Api.GraphQl.Models.Catalog;
using NamEcommerce.Api.GraphQl.Schemes.Catalog.Categories;

namespace NamEcommerce.Api.GraphQl.Schemes.Catalog;

public sealed class CatalogQuery : ObjectGraphType
{
    public CatalogQuery(IServiceProvider services)
    {
        Name = "CatalogQuery";
        Description = "Describes catalog queries";

        Field<CategoryQuery>("category").Resolve(_ => services.GetRequiredService<CategoryQuery>());
    }
}
