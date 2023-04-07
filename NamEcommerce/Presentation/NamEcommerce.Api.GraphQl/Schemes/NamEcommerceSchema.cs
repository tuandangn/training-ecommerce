using GraphQL.Types;
using MediatR;

namespace NamEcommerce.Api.GraphQl.Schemes;

public sealed class NamEcommerceSchema : Schema
{
    public NamEcommerceSchema(IServiceProvider services, NamEcommerceQuery query) : base(services)
    {
        Description = "NamEcommerce GraphQL Schema";
        Query = query;
    }
}
