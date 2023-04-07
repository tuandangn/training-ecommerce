using GraphQL.Types;
using NamEcommerce.Api.GraphQl.Schemes.Catalog;

namespace NamEcommerce.Api.GraphQl.Schemes;

public sealed class NamEcommerceQuery : ObjectGraphType
{
    public NamEcommerceQuery()
    {
        Name = "NamEcommerceQuery";
        Description = "Describes main queries";

        Field<CatalogQuery>("catalog")
            .Description("Describes catalog queries")
            .Resolve(context => context.RequestServices!.GetRequiredService<CatalogQuery>());
    }
}
