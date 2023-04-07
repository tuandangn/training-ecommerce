using GraphQL.Client.Http;
using MediatR;

namespace NamEcommerce.Admin.Client.Queries.Models.GraphQl;

[Serializable]
public record GetGraphQlHttpClient() : IRequest<GraphQLHttpClient>;
