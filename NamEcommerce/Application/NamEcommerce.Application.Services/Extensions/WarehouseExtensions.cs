using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Dtos.Inventory;

namespace NamEcommerce.Application.Services.Extensions;

public static class WarehouseExtensions
{
    public static WarehouseAppDto ToDto(this Warehouse warehouse)
        => new WarehouseAppDto(warehouse.Id)
        {
            Code = warehouse.Code,
            Name = warehouse.Name,
            WarehouseType = (int)warehouse.WarehouseType,
            WarehouseNameKey = $"Enums.{typeof(WarehouseType).FullName}.{warehouse.WarehouseType}",
            PhoneNumber = warehouse.PhoneNumber,
            Address = warehouse.Address,
            ManagerUserId = warehouse.ManagerUserId,
            IsActive = warehouse.IsActive
        };

    public static WarehouseAppDto ToDto(this WarehouseDto dto)
        => new WarehouseAppDto(dto.Id)
        {
            Code = dto.Code,
            Name = dto.Name,
            WarehouseType = dto.WarehouseType,
            WarehouseNameKey = $"Enums.{typeof(WarehouseType).FullName}.{(WarehouseType)dto.WarehouseType}",
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            ManagerUserId = dto.ManagerUserId,
            IsActive = dto.IsActive
        };
}
