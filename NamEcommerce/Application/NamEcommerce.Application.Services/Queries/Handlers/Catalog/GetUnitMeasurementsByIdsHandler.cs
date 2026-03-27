using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetUnitMeasurementsByIdsHandler : IRequestHandler<GetUnitMeasurementsByIds, IEnumerable<UnitMeasurementAppDto>>
{
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;

    public GetUnitMeasurementsByIdsHandler(IEntityDataReader<UnitMeasurement> unitMeasurementDataReader)
    {
        _unitMeasurementDataReader = unitMeasurementDataReader;
    }

    public async Task<IEnumerable<UnitMeasurementAppDto>> Handle(GetUnitMeasurementsByIds request, CancellationToken cancellationToken)
    {
        if (request.Ids.Count() == 0)
            return Enumerable.Empty<UnitMeasurementAppDto>();

        var unitMeasurements = await _unitMeasurementDataReader.GetByIdsAsync(request.Ids);

        return unitMeasurements.Select(unitMeasurement => unitMeasurement.ToDto());
    }
}
