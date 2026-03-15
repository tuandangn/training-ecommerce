using MediatR;
using NamEcommerce.Api.GraphQl.DataLoaders;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;

namespace NamEcommerce.Api.GraphQl.Test;

public sealed class UnitMeasureDataLoaderTests
{
    #region GetAllUnitMeasurementsAsync

    [Fact]
    public async Task GetAllUnitMeasurementsAsync_ReturnsResult()
    {
        var unitMeasurements = new[] { new UnitMeasurementDto(Guid.NewGuid(), "UnitMeasurement 1") };
        var query = new GetAllUnitMeasurements();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(mediator => mediator.Send(query, default)).ReturnsAsync(unitMeasurements);
        var unitMeasurementDataLoader = new UnitMeasurementDataLoader(mediatorMock.Object);

        var result = await unitMeasurementDataLoader.GetAllUnitMeasurementsAsync(default);

        Assert.Equal(unitMeasurements.Length, result.Count());
        Assert.Equal(unitMeasurements, result);
        mediatorMock.Verify();
    }

    #endregion

    #region GetUnitMeasurementByIdAsync

    [Fact]
    public async Task GetUnitMeasurementByIdAsync_ReturnsResult()
    {
        var unitMeasurement = new UnitMeasurementDto(Guid.NewGuid(), "UnitMeasurement 1");
        var query = new GetUnitMeasurementById(unitMeasurement.Id);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(mediator => mediator.Send(query, default)).ReturnsAsync(unitMeasurement);
        var unitMeasurementDataLoader = new UnitMeasurementDataLoader(mediatorMock.Object);

        var result = await unitMeasurementDataLoader.GetUnitMeasurementByIdAsync(default, unitMeasurement.Id);

        Assert.Equal(unitMeasurement, result);
        mediatorMock.Verify();
    }

    #endregion

    #region GetUnitMeasurementsByIdsAsync

    [Fact]
    public async Task GetUnitMeasurementsByIdsAsync_ReturnsResult()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var unitMeasurements = new[] { new UnitMeasurementDto(ids[0], "UnitMeasurement 1"), new UnitMeasurementDto(ids[1], "UnitMeasurement 2") };
        var query = new GetUnitMeasurementsByIds(ids);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(mediator => mediator.Send(query, default)).ReturnsAsync(unitMeasurements);
        var unitMeasurementDataLoader = new UnitMeasurementDataLoader(mediatorMock.Object);

        var result = await unitMeasurementDataLoader.GetUnitMeasurementsByIdsAsync(ids, default);

        Assert.Equal(2, result.Count);
        Assert.Equal(unitMeasurements[0], result[unitMeasurements[0].Id]);
        Assert.Equal(unitMeasurements[1], result[unitMeasurements[1].Id]);
        mediatorMock.Verify();
    }

    #endregion
}
