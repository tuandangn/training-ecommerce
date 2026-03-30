using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Inventory;

namespace NamEcommerce.Application.Contracts.Inventory;

public interface IWarehouseAppService
{
    Task<IPagedDataAppDto<WarehouseAppDto>> GetWarehousesAsync(string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<WarehouseAppDto?> GetWarehouseByIdAsync(Guid id);

    Task<CreateWarehouseResultAppDto> CreateWarehouseAsync(CreateWarehouseAppDto dto);

    Task<UpdateWarehouseResultAppDto> UpdateWarehouseAsync(UpdateWarehouseAppDto dto);

    Task<DeleteWarehouseResultAppDto> DeleteWarehouseAsync(DeleteWarehouseAppDto dto);

    CommonOptionListDto GetAvailableWarehouseTypes();
}
