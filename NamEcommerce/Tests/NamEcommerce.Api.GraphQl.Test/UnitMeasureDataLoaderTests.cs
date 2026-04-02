using MediatR;
using NamEcommerce.Api.GraphQl.DataLoaders;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Api.GraphQl.Test;

public sealed class UnitMeasureDataLoaderTests
{
    #region GetAllUnitMeasurementsAsync

    [Fact]
    public async Task GetAllUnitMeasurementsAsync_ReturnsResult()
    {
        var unitMeasurements = new[] {
            new UnitMeasurementAppDto(Guid.NewGuid()){
                Name = "UnitMeasurement 1"
            }
        };
        var unitMeasurementAppServiceMock = new Mock<IUnitMeasurementAppService>();
        unitMeasurementAppServiceMock.Setup(service => service.GetUnitMeasurementsAsync()).ReturnsAsync(PagedDataAppDto.Create(unitMeasurements));
        var unitMeasurementDataLoader = new UnitMeasurementDataLoader(unitMeasurementAppServiceMock.Object);

        var result = await unitMeasurementDataLoader.GetAllUnitMeasurementsAsync(default);

        Assert.Equal(unitMeasurements.Length, result.Count());
        Assert.Equal(unitMeasurements, result);
        unitMeasurementAppServiceMock.Verify();
    }

    #endregion

    #region GetUnitMeasurementByIdAsync

    [Fact]
    public async Task GetUnitMeasurementByIdAsync_ReturnsResult()
    {
        var unitMeasurement = new UnitMeasurementAppDto(Guid.NewGuid())
        {
            Name = "UnitMeasurement 1"
        };
        var unitMeasurementAppServiceMock = new Mock<IUnitMeasurementAppService>();
        unitMeasurementAppServiceMock.Setup(service => service.GetUnitMeasurementByIdAsync(unitMeasurement.Id)).ReturnsAsync(unitMeasurement);
        var unitMeasurementDataLoader = new UnitMeasurementDataLoader(unitMeasurementAppServiceMock.Object);

        var result = await unitMeasurementDataLoader.GetUnitMeasurementByIdAsync(default, unitMeasurement.Id);

        Assert.Equal(unitMeasurement, result);
        unitMeasurementAppServiceMock.Verify();
    }

    #endregion

    #region GetUnitMeasurementsByIdsAsync

    [Fact]
    public async Task GetUnitMeasurementsByIdsAsync_ReturnsResult()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var unitMeasurements = new[] {
            new UnitMeasurementAppDto(ids[0])
            {
                Name = "UnitMeasurement 1"
            },
            new UnitMeasurementAppDto(ids[1])
            {
                Name = "UnitMeasurement 2"
            }
        };
        var unitMeasurementAppServiceMock = new Mock<IUnitMeasurementAppService>();
        unitMeasurementAppServiceMock.Setup(service => service.GetUnitMeasurementsByIdsAsync(ids)).ReturnsAsync(unitMeasurements);
        var unitMeasurementDataLoader = new UnitMeasurementDataLoader(unitMeasurementAppServiceMock.Object);
        var result = await unitMeasurementDataLoader.GetUnitMeasurementsByIdsAsync(ids, default);

        Assert.Equal(2, result.Count);
        Assert.Equal(unitMeasurements[0], result[unitMeasurements[0].Id]);
        Assert.Equal(unitMeasurements[1], result[unitMeasurements[1].Id]);
        unitMeasurementAppServiceMock.Verify();
    }

    #endregion
}
