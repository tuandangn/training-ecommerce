using NamEcommerce.Application.Shared.Dtos.Catalog;

namespace NamEcommerce.Api.GraphQl.DataLoaders;

public interface IUnitMeasurementDataLoader
{
    Task<IEnumerable<UnitMeasurementDto>> GetAllUnitMeasurementsAsync(CancellationToken cancellationToken);
    Task<UnitMeasurementDto?> GetUnitMeasurementByIdAsync(CancellationToken cancellationToken, Guid? id);
    Task<IDictionary<Guid, UnitMeasurementDto>> GetUnitMeasurementsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
}