using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class UnitMeasurementDataReader
{
    public static Mock<IEntityDataReader<UnitMeasurement>> Empty()
        => EntityDataReader.Create<UnitMeasurement>().WithData(Array.Empty<UnitMeasurement>());
}
