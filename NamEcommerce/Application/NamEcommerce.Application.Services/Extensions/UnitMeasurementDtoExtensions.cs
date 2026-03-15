using NamEcommerce.Application.Shared.Dtos.Catalog;

namespace NamEcommerce.Application.Services.Extensions;

public static class UnitMeasurementDtoExtensions
{
    public static UnitMeasurementDto ToDto(this Domain.Shared.Dtos.Catalog.UnitMeasurementDto category)
        => new(category.Id, category.Name)
        {
            DisplayOrder = category.DisplayOrder
        };
}
