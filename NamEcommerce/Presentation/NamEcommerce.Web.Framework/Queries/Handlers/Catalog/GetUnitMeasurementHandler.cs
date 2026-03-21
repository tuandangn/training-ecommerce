using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetUnitMeasurementHandler : IRequestHandler<GetUnitMeasurementQuery, UnitMeasurementModel?>
{
    private readonly IUnitMeasurementAppService _unitMeasurementAppService;

    public GetUnitMeasurementHandler(IUnitMeasurementAppService unitMeasurementAppService)
    {
        _unitMeasurementAppService = unitMeasurementAppService;
    }

    public async Task<UnitMeasurementModel?> Handle(GetUnitMeasurementQuery request, CancellationToken cancellationToken)
    {
        var unitMeasurement = await _unitMeasurementAppService.GetUnitMeasurementByIdAsync(request.Id);
        if (unitMeasurement == null)
            return null;

        return new UnitMeasurementModel
        {
            Id = unitMeasurement.Id,
            Name = unitMeasurement.Name,
            DisplayOrder = unitMeasurement.DisplayOrder
        };
    }
}
