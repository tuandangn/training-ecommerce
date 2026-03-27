using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Domain.Shared.Services.Catalog;

public interface IUnitMeasurementManager : ICheckNameService
{
    Task<IPagedDataDto<UnitMeasurementDto>> GetUnitMeasurementsAsync(string? keywords, int pageIndex, int pageSize);

    Task<CreateUnitMeasurementResultDto> CreateUnitMeasurementAsync(CreateUnitMeasurementDto dto);

    Task<UpdateUnitMeasurementResultDto> UpdateUnitMeasurementAsync(UpdateUnitMeasurementDto dto);

    Task DeleteUnitMeasurementAsync(Guid id);
}
