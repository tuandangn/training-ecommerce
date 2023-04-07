using GraphQL.Types;

namespace NamEcommerce.Api.GraphQl.Schemes.Catalog;

public sealed class CatalogSchema : Schema
{
    public CatalogSchema(CatalogQuery query)
    {
        Description = "Describes catalog schema";
        Query = query;
    }
}
