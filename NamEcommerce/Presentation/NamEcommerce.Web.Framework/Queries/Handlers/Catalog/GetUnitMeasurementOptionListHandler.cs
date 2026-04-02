using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetUnitMeasurementOptionListHandler : IRequestHandler<GetUnitMeasurementOptionListQuery, EntityOptionListModel>
{
    private readonly IMediator _mediator;

    public GetUnitMeasurementOptionListHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<EntityOptionListModel> Handle(GetUnitMeasurementOptionListQuery request, CancellationToken cancellationToken)
    {
        var unitMeasurementData = await _mediator.Send(new GetUnitMeasurementListQuery
        {
            Keywords = null,
            PageIndex = 0,
            PageSize = int.MaxValue
        }, cancellationToken);

        var optionsData = unitMeasurementData.Data
            .Select(unitMeasurement => new EntityOptionListModel.EntityOptionModel
            {
                Id = unitMeasurement.Id,
                Name = unitMeasurement.Name
            }).ToList();

        return new EntityOptionListModel
        {
            Options = optionsData
        };
    }
}
