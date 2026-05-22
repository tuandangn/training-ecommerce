using MediatR;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class AllocationMarkedAsDirectShipEventHandler(
    IWarehouseManager warehouseManager) : INotificationHandler<AllocationMarkedAsDirectShip>
{
    public async Task Handle(AllocationMarkedAsDirectShip notification, CancellationToken cancellationToken)
    {
        if (await warehouseManager.DirectShipTransitExistsAsync().ConfigureAwait(false))
            return;

        await warehouseManager.CreateWarehouseAsync(new CreateWarehouseDto
        {
            Code = $"{Warehouse.CODE_PREFIX}GIAOTHANG",
            Name = "Kho giao thẳng",
            WarehouseType = WarehouseType.DirectTransit,
            IsActive = true
        }).ConfigureAwait(false);
    }
}
