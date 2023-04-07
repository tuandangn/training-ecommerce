using GraphQL;
using MediatR;
using NamEcommerce.Admin.Client.GraphQl.Queries;
using NamEcommerce.Admin.Client.GraphQl.Responses;
using NamEcommerce.Admin.Client.Models.Catalog;
using NamEcommerce.Admin.Client.Queries.Models.Catalog;
using NamEcommerce.Admin.Client.Queries.Models.GraphQl;

namespace NamEcommerce.Admin.Client.Queries.Handlers.Catalog;

public sealed class GetCategoryHandler : IRequestHandler<GetCategory, CategoryModel>
{
    private readonly IMediator _mediator;

    public GetCategoryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<CategoryModel> Handle(GetCategory request, CancellationToken cancellationToken)
    {
        var graphQlClient = await _mediator.Send(new GetGraphQlHttpClient(), cancellationToken);
        var categoryRequest = new GraphQLRequest
        {
            Query = CategoryQueries.CategoryQuery,
            OperationName = nameof(CategoryQueries.CategoryQuery),
            Variables = new
            {
                id = request.Id
            }
        };
        var result = await graphQlClient.SendQueryAsync<CatalogResponse<CategoryResponseModel>>(categoryRequest, cancellationToken);
        return result.Data.Catalog.Category;
    }
}
