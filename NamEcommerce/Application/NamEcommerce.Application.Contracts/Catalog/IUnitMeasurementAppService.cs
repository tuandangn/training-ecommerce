using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Catalog;

public interface IUnitMeasurementAppService
{
    Task<IPagedDataAppDto<UnitMeasurementAppDto>> GetUnitMeasurementsAsync(string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<IEnumerable<UnitMeasurementAppDto>> GetUnitMeasurementsByIdsAsync(IEnumerable<Guid> ids);

    Task<UnitMeasurementAppDto?> GetUnitMeasurementByIdAsync(Guid id);

    Task<CreateUnitMeasurementResultAppDto> CreateUnitMeasurementAsync(CreateUnitMeasurementAppDto dto);

    Task<UpdateUnitMeasurementResultAppDto> UpdateUnitMeasurementAsync(UpdateUnitMeasurementAppDto dto);

    Task<DeleteUnitMeasurementResultAppDto> DeleteUnitMeasurementAsync(DeleteUnitMeasurementAppDto dto);
}
