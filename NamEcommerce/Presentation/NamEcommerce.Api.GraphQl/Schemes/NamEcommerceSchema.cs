using GraphQL.Types;

namespace NamEcommerce.Api.GraphQl.Schemes;

public sealed class NamEcommerceSchema : Schema
{
    public NamEcommerceSchema(IServiceProvider services) : base(services)
    {
        Query = services.GetRequiredService<NamEcommerceQuery>();
    }
}
