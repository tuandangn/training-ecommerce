using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Services.Extensions;

public static class UnitMeasurementExtensions
{
    public static UnitMeasurementDto ToDto(this UnitMeasurement unitMeasurement)
        => new(unitMeasurement.Id, unitMeasurement.Name)
        {
            DisplayOrder = unitMeasurement.DisplayOrder
        };
}
