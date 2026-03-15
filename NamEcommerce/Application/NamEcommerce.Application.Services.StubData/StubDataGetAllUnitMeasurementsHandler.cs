using MediatR;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;

namespace NamEcommerce.Application.Services.StubData;

public sealed class StubDataGetAllUnitMeasurementsHandler : IRequestHandler<GetAllUnitMeasurements, IEnumerable<UnitMeasurementDto>>
{
    public Task<IEnumerable<UnitMeasurementDto>> Handle(GetAllUnitMeasurements request, CancellationToken cancellationToken)
    {
        IEnumerable<UnitMeasurementDto> allUnitMeasurements = new[]
        {
            new UnitMeasurementDto(Guid.NewGuid(), "Piece")
            {
                DisplayOrder = 1
            },
            new UnitMeasurementDto(Guid.NewGuid(), "Box")
            {
                DisplayOrder = 2 
            },
        };
        return Task.FromResult(allUnitMeasurements);
    }
}
