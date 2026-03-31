using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IWarehouseManager : INameExistCheckingService, ICodeExistCheckingService
{
    Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id);

    Task<IPagedDataDto<WarehouseDto>> GetWarehousesAsync(string? keywords, int pageIndex, int pageSize);

    Task<CreateWarehouseResultDto> CreateWarehouseAsync(CreateWarehouseDto dto);

    Task<UpdateWarehouseResultDto> UpdateWarehouseAsync(UpdateWarehouseDto dto);

    Task DeleteWarehouseAsync(Guid id);
}
