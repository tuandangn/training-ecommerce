using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Domain.Shared.Services;

public interface IUnitMeasurementManager
{
    Task<IPagedDataDto<UnitMeasurementDto>> GetUnitMeasurementsAsync(string? keywords, int pageIndex, int pageSize);

    Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null);

    Task<CreateUnitMeasurementResultDto> CreateUnitMeasurementAsync(CreateUnitMeasurementDto dto);

    Task<UpdateUnitMeasurementResultDto> UpdateUnitMeasurementAsync(UpdateUnitMeasurementDto dto);

    Task DeleteUnitMeasurementAsync(Guid id);
}
