using NamEcommerce.Application.Services.Queries.Handlers.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Domain.Entities.Catalog;

namespace NamEcommerce.Application.Services.Test.Queries.Handlers;

public sealed class GetAllUnitMeasurementsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllUnitMeasurements()
    {
        var firstUnitMeasurement = new UnitMeasurement(Guid.NewGuid(), "UnitMeasurement 1") { DisplayOrder = 1 }; //ordered is first
        var secondUnitMeasurement = new UnitMeasurement(Guid.NewGuid(), "UnitMeasurement 2") { DisplayOrder = 2 }; // ordered is last
        var thirdUnitMeasurement = new UnitMeasurement(Guid.NewGuid(), "UnitMeasurement") { DisplayOrder = 2 }; // ordered is second
        var unorderedUnitMeasurements = new[] { secondUnitMeasurement, firstUnitMeasurement, thirdUnitMeasurement };
        var unitMeasurementManagerMock = UnitMeasurementDataReader.AllUnitMeasurements(unorderedUnitMeasurements);

        var getAllUnitMeasurementsHandler = new GetAllUnitMeasuresHandler(unitMeasurementManagerMock.Object);
        var allUnitMeasurements = await getAllUnitMeasurementsHandler.Handle(default!, default);

        Assert.Equal(3, allUnitMeasurements.Count());
        Assert.Collection(allUnitMeasurements,
            cat => Assert.Equal(firstUnitMeasurement.Id, cat.Id),
            cat => Assert.Equal(thirdUnitMeasurement.Id, cat.Id),
            cat => Assert.Equal(secondUnitMeasurement.Id, cat.Id)
        );
        unitMeasurementManagerMock.Verify();
    }
}
