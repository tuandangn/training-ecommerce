using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

public sealed class DeliveryNoteDeliveringStockHandler(
    IDeliveryNoteAppService deliveryNoteAppService,
    IInventoryStockManager stockManager,
    IInventoryCostingManager inventoryCostingManager) : INotificationHandler<DeliveryNoteDelivering>
{
    public async Task Handle(DeliveryNoteDelivering notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return;

        if (deliveryNote.SourceType != (int)DeliveryNoteSourceType.ToCustomer || deliveryNote.IsDirectShip)
            return;

        var dispatchGroups = deliveryNote.Items
            .GroupBy(item => new
            {
                item.ProductId,
                WarehouseId = item.WarehouseId
            })
            .Select(group => new
            {
                group.Key.ProductId,
                group.Key.WarehouseId,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        foreach (var group in dispatchGroups)
        {
            if (group.WarehouseId == Guid.Empty)
                continue;
            await stockManager.DispatchStockUpToAsync(
                group.ProductId,
                group.WarehouseId,
                group.Quantity,
                deliveryNote.Id,
                Guid.Empty,
                $"Xuat hang cho phieu xuat {deliveryNote.Code}",
                releaseReservedStock: true).ConfigureAwait(false);
        }

        foreach (var item in deliveryNote.Items)
        {
            await inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
            {
                ProductId = item.ProductId,
                WarehouseId = item.WarehouseId,
                Quantity = item.Quantity,
                MovementType = InventoryCostMovementType.SaleDispatch,
                ReferenceType = InventoryCostReferenceType.SalesOrder,
                ReferenceId = deliveryNote.Id,
                ReferenceItemId = item.Id,
                OccurredAtUtc = DateTime.UtcNow
            }).ConfigureAwait(false);
        }
    }
}
