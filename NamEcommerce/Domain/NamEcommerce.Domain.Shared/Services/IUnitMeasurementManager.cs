using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Shared.Services;

public interface IUnitMeasurementManager
{
    Task<UnitMeasurementDto> CreateUnitMeasurementAsync(CreateUnitMeasurementDto dto);

    Task<UnitMeasurementDto> UpdateUnitMeasurementAsync(UpdateUnitMeasurementDto dto);

    Task DeleteUnitMeasurementAsync(Guid id);
}
