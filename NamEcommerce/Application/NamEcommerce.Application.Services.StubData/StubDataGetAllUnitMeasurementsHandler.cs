using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;

namespace NamEcommerce.Application.Services.StubData;

public sealed class StubDataGetAllUnitMeasurementsHandler : IRequestHandler<GetAllUnitMeasurements, IEnumerable<UnitMeasurementAppDto>>
{
    public Task<IEnumerable<UnitMeasurementAppDto>> Handle(GetAllUnitMeasurements request, CancellationToken cancellationToken)
    {
        IEnumerable<UnitMeasurementAppDto> allUnitMeasurements = new[]
        {
            new UnitMeasurementAppDto(Guid.NewGuid())
            {
                Name = "Piece",
                DisplayOrder = 1
            },
            new UnitMeasurementAppDto(Guid.NewGuid())
            {
                Name = "Box",
                DisplayOrder = 2 
            },
        };
        return Task.FromResult(allUnitMeasurements);
    }
}
