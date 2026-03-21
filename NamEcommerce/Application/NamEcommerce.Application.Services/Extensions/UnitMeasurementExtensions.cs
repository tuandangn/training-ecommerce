using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Application.Services.Extensions;

public static class UnitMeasurementExtensions
{
    public static UnitMeasurementAppDto ToDto(this UnitMeasurement unitMeasurement)
        => new UnitMeasurementAppDto(unitMeasurement.Id)
        {
            Name = unitMeasurement.Name,
            DisplayOrder = unitMeasurement.DisplayOrder
        };

    public static UnitMeasurementAppDto ToDto(this UnitMeasurementDto dto)
        => new UnitMeasurementAppDto(dto.Id)
        {
            Name = dto.Name,
            DisplayOrder = dto.DisplayOrder
        };
}
