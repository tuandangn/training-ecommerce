using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class UnitMeasurementDataReader
{
    public static Mock<IEntityDataReader<UnitMeasurement>> Empty()
        => EntityDataReader.Create<UnitMeasurement>().WithData(Array.Empty<UnitMeasurement>());

    public static Mock<IEntityDataReader<UnitMeasurement>> WithData(params UnitMeasurement[] unitMeasurements)
        => EntityDataReader.Create<UnitMeasurement>().WithData(unitMeasurements);
    public static Mock<IEntityDataReader<UnitMeasurement>> HasOne(UnitMeasurement unitMeasurement)
        => EntityDataReader.Create<UnitMeasurement>().WithData(unitMeasurement);

    public static Mock<IEntityDataReader<UnitMeasurement>> NotFound(Guid id)
        => EntityDataReader.Create<UnitMeasurement>().WhenCall(reader => reader.GetByIdAsync(id), (UnitMeasurement?)null);

    public static Mock<IEntityDataReader<UnitMeasurement>> UnitMeasurementById(Guid id, UnitMeasurement unitMeasurement)
        => EntityDataReader.Create<UnitMeasurement>().WhenCall(reader => reader.GetByIdAsync(id), unitMeasurement);
    public static Mock<IEntityDataReader<UnitMeasurement>> UnitMeasurementById(this Mock<IEntityDataReader<UnitMeasurement>> mock, Guid id, UnitMeasurement unitMeasurement)
        => mock.WhenCall(reader => reader.GetByIdAsync(id), unitMeasurement);
}
