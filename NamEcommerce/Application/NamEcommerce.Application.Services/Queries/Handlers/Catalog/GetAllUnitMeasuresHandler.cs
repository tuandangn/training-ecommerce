using MediatR;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetAllUnitMeasuresHandler : IRequestHandler<GetAllUnitMeasurements, IEnumerable<UnitMeasurementDto>>
{
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;

    public GetAllUnitMeasuresHandler(IEntityDataReader<UnitMeasurement> categoryDataReader)
    {
        _unitMeasurementDataReader = categoryDataReader;
    }

    public async Task<IEnumerable<UnitMeasurementDto>> Handle(GetAllUnitMeasurements request, CancellationToken cancellationToken)
    {
        var unitMeasurements = await _unitMeasurementDataReader.GetAllAsync().ConfigureAwait(false);
        return unitMeasurements.OrderBy(unitMeasurement => unitMeasurement.DisplayOrder)
            .ThenBy(unitMeasurement => unitMeasurement.Name)
            .Select(unitMeasurement => unitMeasurement.ToDto());
    }
}
