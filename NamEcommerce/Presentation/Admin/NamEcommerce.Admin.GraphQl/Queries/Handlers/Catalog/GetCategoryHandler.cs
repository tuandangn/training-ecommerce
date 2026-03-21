using GraphQL;
using MediatR;
using NamEcommerce.Admin.Contracts.Models.Catalog;
using NamEcommerce.Admin.Contracts.Models.Common;
using NamEcommerce.Admin.Contracts.Queries.Models.Catalog;
using NamEcommerce.Admin.GraphQl.Common;
using NamEcommerce.Admin.GraphQl.Responses;
using NamEcommerce.Admin.GraphQl.TextQueries;

namespace NamEcommerce.Admin.GraphQl.Queries.Handlers.Catalog;

public sealed class GetCategoryHandler : IRequestHandler<GetCategory, ResponseModel<CategoryModel?>>
{
    private readonly IMediator _mediator;

    public GetCategoryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ResponseModel<CategoryModel?>> Handle(GetCategory request, CancellationToken cancellationToken)
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
        try
        {
            var response = await graphQlClient.SendQueryAsync<CatalogResponse<CategoryResponseModel>>(categoryRequest, cancellationToken);
            if (response.Errors?.Length > 0)
            {
                var errorMessage = string.Join(", ", response.Errors.Select(error => error.Message));
                return ResponseModel.Failed<CategoryModel?>(errorMessage);
            }
            return ResponseModel.Success(response.Data?.Catalog.Category);
        }
        catch (Exception ex)
        {
            return ResponseModel.Failed<CategoryModel?>(ex.Message);
        }
    }
}
