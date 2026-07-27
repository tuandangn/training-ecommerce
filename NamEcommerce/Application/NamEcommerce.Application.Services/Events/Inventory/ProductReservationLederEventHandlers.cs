using MediatR;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.Inventory;

public sealed class ProductReservationLederCreatedEventHandler(IEntityDataReader<ProductReservationLedger> productReservationLedgerDataReader,
    IInventoryStockManager inventoryStockManager, IWarehouseManager warehouseManager) : INotificationHandler<ProductReservationLedgerCreated>
{
    public async Task Handle(ProductReservationLedgerCreated notification, CancellationToken cancellationToken)
    {
        var productReservationLedger = await productReservationLedgerDataReader.GetByIdAsync(notification.ProductReservationLedgerId).ConfigureAwait(false);
        if (productReservationLedger is null)
            return;

        var inventoryStocks = await inventoryStockManager.GetInventoryStocksForProductAsync(notification.ProductId);
        if (inventoryStocks.Any())
            return;

        var warehouses = await warehouseManager.GetWarehousesAsync(0, int.MaxValue, null).ConfigureAwait(false);
        foreach (var warehouse in warehouses)
        {
            if (warehouse.WarehouseType == WarehouseType.DirectTransit)
                continue;

            await inventoryStockManager.InitializeStockAsync(notification.ProductId, warehouse.Id);
        }
    }
}

