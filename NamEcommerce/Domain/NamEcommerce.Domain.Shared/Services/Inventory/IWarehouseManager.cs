using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IWarehouseManager : INameExistCheckingService, ICodeExistCheckingService
{
    Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id);

    Task<IPagedDataDto<WarehouseDto>> GetWarehousesAsync(int pageIndex, int pageSize, string? keywords = null, WarehouseType[]? types = null);

    Task<CreateWarehouseResultDto> CreateWarehouseAsync(CreateWarehouseDto dto);

    Task<UpdateWarehouseResultDto> UpdateWarehouseAsync(UpdateWarehouseDto dto);

    Task DeleteWarehouseAsync(Guid id);

    Task<bool> DirectShipTransitExistsAsync();
}
