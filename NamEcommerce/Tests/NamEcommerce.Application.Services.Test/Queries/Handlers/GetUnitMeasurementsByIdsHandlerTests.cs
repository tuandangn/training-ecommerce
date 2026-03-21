using NamEcommerce.Application.Contracts.Queries.Catalog;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Application.Services.Queries.Handlers.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Domain.Entities.Catalog;

namespace NamEcommerce.Application.Services.Test.Queries.Handlers;

public sealed class GetUnitMeasurementsByIdsHandlerTests
{
    [Fact]
    public async Task Handle_IdsIsEmpty_ReturnEmpty()
    {
        var request = new GetUnitMeasurementsByIds(Enumerable.Empty<Guid>());
        var handler = new GetUnitMeasurementsByIdsHandler(null!);

        var emptyResult = await handler.Handle(request, default);

        Assert.Empty(emptyResult);
    }

    [Fact]
    public async Task Handle_ReturnResult()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var unitMeasurements = new[]
        {
            new UnitMeasurement(ids[0], "UnitMeasurement 1"),
            new UnitMeasurement(ids[1], "UnitMeasurement 2")
        };
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.UnitMeasurementsByIds(
            ids, unitMeasurements
        );
        var request = new GetUnitMeasurementsByIds(ids);
        var handler = new GetUnitMeasurementsByIdsHandler(unitMeasurementDataReaderMock.Object);

        var result = await handler.Handle(request, default);

        Assert.Equal(unitMeasurements[0].ToDto(), result.ElementAt(0));
        Assert.Equal(unitMeasurements[1].ToDto(), result.ElementAt(1));
        unitMeasurementDataReaderMock.Verify();
    }
}
