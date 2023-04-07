using GraphQL;
using MediatR;
using NamEcommerce.Admin.Client.GraphQl.Queries;
using NamEcommerce.Admin.Client.Models.Catalog;
using NamEcommerce.Admin.Client.Queries.Models.Catalog;
using NamEcommerce.Admin.Client.Queries.Models.GraphQl;

namespace NamEcommerce.Admin.Client.Queries.Handlers.Catalog;

public sealed class GetCategoryListHandler : IRequestHandler<GetCategoryList, CategoryListModel>
{
    private readonly IMediator _mediator;

    public GetCategoryListHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<CategoryListModel> Handle(GetCategoryList request, CancellationToken cancellationToken)
    {
        var graphQlClient = await _mediator.Send(new GetGraphQlHttpClient(), cancellationToken);
        var categoryListRequest = new GraphQLRequest
        {
            Query = CategoryQueries.CategoryListQuery,
            OperationName = nameof(CategoryQueries.CategoryListQuery)
        };
        try
        {
            var result = await graphQlClient.SendQueryAsync<object>(categoryListRequest, cancellationToken);
        }catch(Exception ex)
        {

        }

        throw new NotImplementedException();
    }
}
