using MediatR;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetUnitMeasurementsByIdsHandler : IRequestHandler<GetUnitMeasurementsByIds, IEnumerable<UnitMeasurementDto>>
{
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;

    public GetUnitMeasurementsByIdsHandler(IEntityDataReader<UnitMeasurement> unitMeasurementDataReader)
    {
        _unitMeasurementDataReader = unitMeasurementDataReader;
    }

    public async Task<IEnumerable<UnitMeasurementDto>> Handle(GetUnitMeasurementsByIds request, CancellationToken cancellationToken)
    {
        if (request.Ids.Count() == 0)
            return Enumerable.Empty<UnitMeasurementDto>();

        var unitMeasurements = await _unitMeasurementDataReader.GetByIdsAsync(request.Ids);

        return unitMeasurements.Select(unitMeasurement => unitMeasurement.ToDto());
    }
}
