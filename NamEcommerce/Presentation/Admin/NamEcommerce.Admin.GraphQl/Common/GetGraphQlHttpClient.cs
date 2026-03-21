using GraphQL.Client.Http;
using MediatR;

namespace NamEcommerce.Admin.GraphQl.Common;

[Serializable]
public record GetGraphQlHttpClient() : IRequest<GraphQLHttpClient>;
