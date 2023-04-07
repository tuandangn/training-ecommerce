using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using MediatR;
using NamEcommerce.Admin.Client.GraphQl;
using NamEcommerce.Admin.Client.Queries.Models.GraphQl;

namespace NamEcommerce.Admin.Client.Queries.Handlers.GraphQl;

public sealed class GetGraphQlHttpClientHandler : IRequestHandler<GetGraphQlHttpClient, GraphQLHttpClient>
{
    private readonly GraphQlOptions _graphQlOptions;

    public GetGraphQlHttpClientHandler(GraphQlOptions graphQlOptions)
    {
        _graphQlOptions = graphQlOptions;
    }

    public Task<GraphQLHttpClient> Handle(GetGraphQlHttpClient request, CancellationToken cancellationToken)
    {
        var graphQlClient = new GraphQLHttpClient(new GraphQLHttpClientOptions
        {
            EndPoint = new Uri(_graphQlOptions.Endpoint, UriKind.Absolute)
        }, new SystemTextJsonSerializer());

        return Task.FromResult(graphQlClient);
    }
}
