using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Test.Helpers;

public static class UnitMeasurementManager
{
    public static Mock<IUnitMeasurementManager> WhenGetUnitMeasurementsReturns(string keywords, int pageIndex, int pageSize, IPagedDataDto<UnitMeasurementDto> @return)
    {
        var mock = new Mock<IUnitMeasurementManager>();
        mock.Setup(r => r.GetUnitMeasurementsAsync(keywords, pageIndex, pageSize)).ReturnsAsync(@return).Verifiable();

        return mock;
    }

    public static Mock<IUnitMeasurementManager> SetUsernameExists(string name, bool exists)
    {
        var mock = new Mock<IUnitMeasurementManager>();
        mock.Setup(r => r.DoesNameExistAsync(name, null)).ReturnsAsync(exists).Verifiable();

        return mock;
    }

    public static Mock<IUnitMeasurementManager> CreateUnitMeasurementReturns(this Mock<IUnitMeasurementManager> mock, CreateUnitMeasurementDto dto, CreateUnitMeasurementResultDto @return)
    {
        mock.Setup(r => r.CreateUnitMeasurementAsync(dto)).ReturnsAsync(@return).Verifiable();

        return mock;
    }

    public static Mock<IUnitMeasurementManager> CanDeleteUnitMeasurement(Guid id)
    {
        var mock = new Mock<IUnitMeasurementManager>();

        mock.Setup(r => r.DeleteUnitMeasurementAsync(id)).Returns(Task.CompletedTask).Verifiable();

        return mock;
    }
}
