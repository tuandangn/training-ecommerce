using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetAllUnitMeasuresHandler : IRequestHandler<GetAllUnitMeasurements, IEnumerable<UnitMeasurementAppDto>>
{
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;

    public GetAllUnitMeasuresHandler(IEntityDataReader<UnitMeasurement> categoryDataReader)
    {
        _unitMeasurementDataReader = categoryDataReader;
    }

    public async Task<IEnumerable<UnitMeasurementAppDto>> Handle(GetAllUnitMeasurements request, CancellationToken cancellationToken)
    {
        var unitMeasurements = await _unitMeasurementDataReader.GetAllAsync().ConfigureAwait(false);
        return unitMeasurements.OrderBy(unitMeasurement => unitMeasurement.DisplayOrder)
            .ThenBy(unitMeasurement => unitMeasurement.Name)
            .Select(unitMeasurement => unitMeasurement.ToDto());
    }
}
