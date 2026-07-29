using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class QuickSaleDeliverRequestedHandler(
    IDeliveryNoteAppService deliveryNoteAppService,
    IInventoryStockManager stockManager,
    IInventoryCostingManager inventoryCostingManager,
    IDeliveryNoteManager deliveryNoteManager,
    IOrderManager orderManager) : INotificationHandler<DeliveryRequested>
{
    public async Task Handle(DeliveryRequested notification, CancellationToken cancellationToken)
    {
        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return;

        var dispatchGroups = deliveryNote.Items
            .GroupBy(item => new
            {
                item.ProductId,
                WarehouseId = item.WarehouseId
            })
            .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, Quantity = g.Sum(i => i.Quantity) })
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
                OccurredAtUtc = notification.RequestedAtUtc
            }).ConfigureAwait(false);
        }

        await deliveryNoteManager.MarkReceivedByCustomerAsync(
            notification.DeliveryNoteId,
            notification.RequestedAtUtc,
            receiverName: null,
            note: null).ConfigureAwait(false);

        await orderManager.CompleteOrderAsync(new CompleteOrderDto
        {
            OrderId = notification.OrderId
        }).ConfigureAwait(false);
    }
}
