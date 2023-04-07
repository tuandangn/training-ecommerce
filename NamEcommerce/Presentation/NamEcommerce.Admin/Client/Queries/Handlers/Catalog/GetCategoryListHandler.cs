using GraphQL;
using MediatR;
using NamEcommerce.Admin.Client.GraphQl.Queries;
using NamEcommerce.Admin.Client.GraphQl.Responses;
using NamEcommerce.Admin.Client.Models.Catalog;
using NamEcommerce.Admin.Client.Models.Common;
using NamEcommerce.Admin.Client.Queries.Models.Catalog;
using NamEcommerce.Admin.Client.Queries.Models.GraphQl;

namespace NamEcommerce.Admin.Client.Queries.Handlers.Catalog;

public sealed class GetCategoryListHandler : IRequestHandler<GetCategoryList, ResponseModel<CategoryListModel>>
{
    private readonly IMediator _mediator;

    public GetCategoryListHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ResponseModel<CategoryListModel>> Handle(GetCategoryList request, CancellationToken cancellationToken)
    {
        var graphQlClient = await _mediator.Send(new GetGraphQlHttpClient(), cancellationToken);
        var categoryListRequest = new GraphQLRequest
        {
            Query = CategoryQueries.CategoryListQuery,
            OperationName = nameof(CategoryQueries.CategoryListQuery)
        };
        try
        {
            var response = await graphQlClient.SendQueryAsync<CatalogResponse<CategoriesResponseModel>>(categoryListRequest, cancellationToken);
            if (response.Errors?.Length > 0)
            {
                var errorMessage = string.Join(", ", response.Errors.Select(error => error.Message));
                return ResponseModel.Failed<CategoryListModel>(errorMessage);
            }
            var categories = response.Data.Catalog.Categories;
            var model = new CategoryListModel
            {
                Categories = categories
            };
            return ResponseModel.Success(model);
        }
        catch (Exception ex)
        {
            return ResponseModel.Failed<CategoryListModel>(ex.Message);
        }
    }
}
