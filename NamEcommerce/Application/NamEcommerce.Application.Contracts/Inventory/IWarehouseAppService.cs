using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Inventory;

namespace NamEcommerce.Application.Contracts.Inventory;

public interface IWarehouseAppService
{
    Task<IPagedDataAppDto<WarehouseAppDto>> GetWarehousesAsync(int pageIndex, int pageSize, string? keywords = null, 
        bool includeDirectTransit = false, bool includeDamaged = false);

    Task<WarehouseAppDto?> GetWarehouseByIdAsync(Guid id);

    Task<CreateWarehouseResultAppDto> CreateWarehouseAsync(CreateWarehouseAppDto dto);

    Task<UpdateWarehouseResultAppDto> UpdateWarehouseAsync(UpdateWarehouseAppDto dto);

    Task<DeleteWarehouseResultAppDto> DeleteWarehouseAsync(DeleteWarehouseAppDto dto);

    CommonOptionListDto GetAvailableWarehouseTypes();
}
