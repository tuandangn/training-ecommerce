using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;

namespace NamEcommerce.Domain.Shared.Services.Finance;

public interface IFixedAssetManager
{
    Task<FixedAssetDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<FixedAssetDto>> GetAllAsync(FixedAssetStatus? status = null);
    Task<FixedAssetDto> CreateAsync(CreateFixedAssetDto dto);
    Task<FixedAssetDto> UpdateAsync(Guid id, string name, string? description, string? note, FixedAssetCostCenter costCenter);
    Task DisposeAsync(Guid id, DateTime disposedOnUtc);
    Task<string> GenerateCodeAsync();
}
