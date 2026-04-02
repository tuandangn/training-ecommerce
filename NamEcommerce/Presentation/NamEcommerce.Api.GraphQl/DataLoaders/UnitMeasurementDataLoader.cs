using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Api.GraphQl.DataLoaders;

public sealed class UnitMeasurementDataLoader : IUnitMeasurementDataLoader
{
    public const string GET_ALL = "UnitMeasurementDataLoader.GetAll";
    public const string GET_BY_ID = "UnitMeasurementDataLoader.GetById";

    private readonly IUnitMeasurementAppService _unitMeasurementAppService;

    public UnitMeasurementDataLoader(IUnitMeasurementAppService unitMeasurementAppService)
    {
        _unitMeasurementAppService = unitMeasurementAppService;
    }

    public async Task<IEnumerable<UnitMeasurementAppDto>> GetAllUnitMeasurementsAsync(CancellationToken cancellationToken)
    {
        var unitMeasurementData = await _unitMeasurementAppService.GetUnitMeasurementsAsync().ConfigureAwait(false);
        return unitMeasurementData;
    }

    public async Task<UnitMeasurementAppDto?> GetUnitMeasurementByIdAsync(CancellationToken cancellationToken, Guid? id)
    {
        if (!id.HasValue)
            return null;
        return await _unitMeasurementAppService.GetUnitMeasurementByIdAsync(id.Value).ConfigureAwait(false);
    }

    public async Task<IDictionary<Guid, UnitMeasurementAppDto>> GetUnitMeasurementsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var categories = await _unitMeasurementAppService.GetUnitMeasurementsByIdsAsync(ids).ConfigureAwait(false);

        return categories.ToDictionary(category => category.Id);
    }
}
