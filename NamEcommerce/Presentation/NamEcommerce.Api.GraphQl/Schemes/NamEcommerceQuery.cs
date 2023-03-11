using GraphQL.Types;
using NamEcommerce.Api.GraphQl.Schemes.Catalog;

namespace NamEcommerce.Api.GraphQl.Schemes;

public sealed class NamEcommerceQuery : ObjectGraphType
{
    public NamEcommerceQuery(IServiceProvider services)
    {
        Name = "NamEcommerceQuery";
        Description = "Describes main queries";

        Field<CatalogQuery>("catalog").Resolve(_ => services.GetRequiredService<CatalogQuery>());
    }
}
