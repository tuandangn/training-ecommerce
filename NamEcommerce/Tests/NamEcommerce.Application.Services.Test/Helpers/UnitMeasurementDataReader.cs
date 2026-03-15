using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;
using NamEcommerce.TestHelper;

namespace NamEcommerce.Application.Services.Test.Helpers;

public static class UnitMeasurementDataReader
{
    public static Mock<IEntityDataReader<UnitMeasurement>> AllUnitMeasurements(params UnitMeasurement[] unitMeasurements)
        => EntityDataReader.Create<UnitMeasurement>()
            .WhenCall(reader => reader.GetAllAsync(), unitMeasurements);

    public static Mock<IEntityDataReader<UnitMeasurement>> UnitMeasurementsByIds(Guid[] ids, params UnitMeasurement[] unitMeasurements)
        => EntityDataReader.Create<UnitMeasurement>()
            .WhenCall(reader => reader.GetByIdsAsync(ids), unitMeasurements);
}
