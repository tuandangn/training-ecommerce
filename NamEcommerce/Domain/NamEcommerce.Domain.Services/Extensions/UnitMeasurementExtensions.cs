using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Services.Extensions;

public static class UnitMeasurementExtensions
{
    public static UnitMeasurementDto ToDto(this UnitMeasurement unitMeasurement)
        => new UnitMeasurementDto(unitMeasurement.Id)
        {
            Name = unitMeasurement.Name,
            DisplayOrder = unitMeasurement.DisplayOrder
        };
}
