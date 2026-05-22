using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

public sealed class DeliveryNoteDeliveredStockHandler(
    IDeliveryNoteAppService deliveryNoteAppService,
    IInventoryStockManager stockManager,
    IInventoryCostingManager inventoryCostingManager) : INotificationHandler<DeliveryNoteDelivered>
{
    public async Task Handle(DeliveryNoteDelivered notification, CancellationToken cancellationToken)
    {
        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null) return;

        var releaseReservedStock = deliveryNote.OrderId != Guid.Empty
            && deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToCustomer;

        foreach (var item in deliveryNote.Items)
        {
            await stockManager.DispatchStockAsync(
                item.ProductId,
                deliveryNote.WarehouseId,
                item.Quantity,
                deliveryNote.Id,
                Guid.Empty,
                $"Xuất hàng cho phiếu xuất {deliveryNote.Code}",
                releaseReservedStock: releaseReservedStock).ConfigureAwait(false);

            await inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
            {
                ProductId = item.ProductId,
                WarehouseId = deliveryNote.WarehouseId,
                Quantity = item.Quantity,
                MovementType = deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToVendorReturn
                    ? InventoryCostMovementType.VendorReturn
                    : InventoryCostMovementType.SaleDispatch,
                ReferenceType = deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToVendorReturn
                    ? InventoryCostReferenceType.VendorReturn
                    : InventoryCostReferenceType.SalesOrder,
                ReferenceId = deliveryNote.Id,
                ReferenceItemId = item.Id,
                OccurredAtUtc = deliveryNote.DeliveredOnUtc ?? DateTime.UtcNow
            }).ConfigureAwait(false);
        }
    }
}
