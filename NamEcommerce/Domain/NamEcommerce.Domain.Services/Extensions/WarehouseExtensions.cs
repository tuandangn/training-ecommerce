using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Services.Extensions;

public static class WarehouseExtensions
{
    public static WarehouseDto ToDto(this Warehouse warehouse)
        => new WarehouseDto(warehouse.Id)
        {
            Code = warehouse.Code,
            Name = warehouse.Name,
            PhoneNumber = warehouse.PhoneNumber,
            Address = warehouse.Address,
            ManagerUserId = warehouse.ManagerUserId,
            WarehouseType =  warehouse.WarehouseType,
            WarehouseNameKey = $"Enums.{typeof(WarehouseType).FullName}.{warehouse.WarehouseType}",
            IsActive = warehouse.IsActive
        };
}
