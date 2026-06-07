using NamEcommerce.Application.Contracts.Dtos.Finance;

namespace NamEcommerce.Application.Contracts.Finance;

public interface IFixedAssetAppService
{
    Task<IReadOnlyList<FixedAssetAppDto>> GetFixedAssetsAsync(int? status = null);
    Task<FixedAssetAppDto?> GetFixedAssetByIdAsync(Guid id);
    Task<IReadOnlyList<DepreciationScheduleItemAppDto>> GetDepreciationScheduleAsync(Guid id);
    Task<FixedAssetOperationResultAppDto> CreateFixedAssetAsync(CreateFixedAssetAppDto dto);
    Task<FixedAssetOperationResultAppDto> UpdateFixedAssetAsync(Guid id, string name, string? description, string? note, int costCenter);
    Task<FixedAssetOperationResultAppDto> DisposeFixedAssetAsync(Guid id, DateTime disposedOnUtc);
}
