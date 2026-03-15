using GraphQL;
using MediatR;
using NamEcommerce.Admin.Client.GraphQl.Queries;
using NamEcommerce.Admin.Client.GraphQl.Responses;
using NamEcommerce.Admin.Client.Models.Catalog;
using NamEcommerce.Admin.Client.Models.Common;
using NamEcommerce.Admin.Client.Queries.Models.Catalog;
using NamEcommerce.Admin.Client.Queries.Models.GraphQl;

namespace NamEcommerce.Admin.Client.Queries.Handlers.Catalog;

public sealed class GetUnitMeasurementListHandler : IRequestHandler<GetUnitMeasurementList, ResponseModel<UnitMeasurementListModel>>
{
    private readonly IMediator _mediator;

    public GetUnitMeasurementListHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ResponseModel<UnitMeasurementListModel>> Handle(GetUnitMeasurementList request, CancellationToken cancellationToken)
    {
        var graphQlClient = await _mediator.Send(new GetGraphQlHttpClient(), cancellationToken);
        var unitMeasurementListRequest = new GraphQLRequest
        {
            Query = UnitMeasurementQueries.UnitMeasurementListQuery,
            OperationName = nameof(UnitMeasurementQueries.UnitMeasurementListQuery)
        };
        try
        {
            var response = await graphQlClient.SendQueryAsync<CatalogResponse<UnitMeasurementsResponseModel>>(unitMeasurementListRequest, cancellationToken);
            if (response.Errors?.Length > 0)
            {
                var errorMessage = string.Join(", ", response.Errors.Select(error => error.Message));
                return ResponseModel.Failed<UnitMeasurementListModel>(errorMessage);
            }
            var unitMeasurements = response.Data.Catalog.UnitMeasurements;
            var model = new UnitMeasurementListModel
            {
                UnitMeasurements = unitMeasurements
            };
            return ResponseModel.Success(model);
        }
        catch (Exception ex)
        {
            return ResponseModel.Failed<UnitMeasurementListModel>(ex.Message);
        }
    }
}
