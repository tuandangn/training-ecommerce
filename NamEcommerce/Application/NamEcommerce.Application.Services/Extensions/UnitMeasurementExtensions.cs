using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Entities.Catalog;

namespace NamEcommerce.Application.Services.Extensions;

public static class UnitMeasurementExtensions
{
    public static UnitMeasurementDto ToDto(this UnitMeasurement unitMeasurement)
        => new(unitMeasurement.Id, unitMeasurement.Name)
        {
            DisplayOrder = unitMeasurement.DisplayOrder
        };
}
