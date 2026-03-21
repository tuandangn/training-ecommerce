using GraphQL;
using MediatR;
using NamEcommerce.Admin.Contracts.Models.Catalog;
using NamEcommerce.Admin.Contracts.Models.Common;
using NamEcommerce.Admin.Contracts.Queries.Models.Catalog;
using NamEcommerce.Admin.GraphQl.Common;
using NamEcommerce.Admin.GraphQl.Responses;
using NamEcommerce.Admin.GraphQl.TextQueries;

namespace NamEcommerce.Admin.Client.Queries.Handlers.Catalog;

public sealed class GetUnitMeasurementHandler : IRequestHandler<GetUnitMeasurement, ResponseModel<UnitMeasurementModel?>>
{
    private readonly IMediator _mediator;

    public GetUnitMeasurementHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ResponseModel<UnitMeasurementModel?>> Handle(GetUnitMeasurement request, CancellationToken cancellationToken)
    {
        var graphQlClient = await _mediator.Send(new GetGraphQlHttpClient(), cancellationToken);
        var unitMeasurementRequest = new GraphQLRequest
        {
            Query = UnitMeasurementQueries.UnitMeasurementQuery,
            OperationName = nameof(UnitMeasurementQueries.UnitMeasurementQuery),
            Variables = new
            {
                id = request.Id
            }
        };
        try
        {
            var response = await graphQlClient.SendQueryAsync<CatalogResponse<UnitMeasurementResponseModel>>(unitMeasurementRequest, cancellationToken);
            if (response.Errors?.Length > 0)
            {
                var errorMessage = string.Join(", ", response.Errors.Select(error => error.Message));
                return ResponseModel.Failed<UnitMeasurementModel?>(errorMessage);
            }
            return ResponseModel.Success(response.Data?.Catalog.UnitMeasurement);
        }
        catch (Exception ex)
        {
            return ResponseModel.Failed<UnitMeasurementModel?>(ex.Message);
        }
    }
}
