using MediatR;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

public sealed class DeliveryNoteCreatedHandler(IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IEntityDataReader<Order> orderDataReader, IProductReservationManager productReservationManager,
    IInventoryStockManager inventoryStockManager) : INotificationHandler<DeliveryNoteCreated>
{
    public async Task Handle(DeliveryNoteCreated notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        var deliveryNote = await deliveryNoteDataReader.GetByIdAsync(notification.DeliveryNoteId, default).ConfigureAwait(false);
        if (deliveryNote is null)
            return;

        if (deliveryNote.SourceType != DeliveryNoteSourceType.ToCustomer && deliveryNote.SourceType != DeliveryNoteSourceType.DirectShipToCustomer)
            return;

        var productGroups = deliveryNote.Items.GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToList();
        var warehouseGroups = deliveryNote.Items
            .GroupBy(item => new
            {
                item.ProductId,
                WarehouseId = item.WarehouseId == Guid.Empty ? deliveryNote.WarehouseId : item.WarehouseId
            })
            .Select(group => new { group.Key.ProductId, group.Key.WarehouseId, Quantity = group.Sum(item => item.Quantity) })
            .ToList();

        //release global stock
        if (deliveryNote.OrderId != Guid.Empty)
        {
            var order = await orderDataReader.GetByIdAsync(deliveryNote.OrderId, default).ConfigureAwait(false);
            if (order is not null)
            {
                foreach (var group in productGroups)
                {
                    var reservedQuantity = await productReservationManager.GetReservedForOrderAsync(group.ProductId, order.Id).ConfigureAwait(false);
                    var releasingQuantity = Math.Min(group.Quantity, reservedQuantity);
                    if (releasingQuantity <= 0)
                        continue;

                    await productReservationManager.ReleaseAsync(group.ProductId, releasingQuantity, order.Id, ProductReservationReason.DeliveryNoteCreated, deliveryNote.Id)
                        .ConfigureAwait(false);
                }
            }
        }

        //reserve warehouse stock
        foreach (var group in warehouseGroups)
        {
            await inventoryStockManager.ReserveStockAsync(
                group.ProductId,
                group.WarehouseId,
                group.Quantity,
                deliveryNote.Id,
                Guid.Empty,
                $"Giữ hàng cho phiếu xuất {deliveryNote.Code}").ConfigureAwait(false);
        }
    }
}
