using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Api.GraphQl.DataLoaders;

public interface IUnitMeasurementDataLoader
{
    Task<IEnumerable<UnitMeasurementAppDto>> GetAllUnitMeasurementsAsync(CancellationToken cancellationToken);
    Task<UnitMeasurementAppDto?> GetUnitMeasurementByIdAsync(CancellationToken cancellationToken, Guid? id);
    Task<IDictionary<Guid, UnitMeasurementAppDto>> GetUnitMeasurementsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
}