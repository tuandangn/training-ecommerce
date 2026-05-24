using MediatR;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
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

        var deliveryNote = await deliveryNoteDataReader.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return;

        if (deliveryNote.SourceType != DeliveryNoteSourceType.ToCustomer && deliveryNote.SourceType != DeliveryNoteSourceType.DirectShipToCustomer)
            return;

        var productGroups = deliveryNote.Items.GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToList();

        //release global stock
        if (deliveryNote.OrderId != Guid.Empty)
        {
            var order = await orderDataReader.GetByIdAsync(deliveryNote.OrderId).ConfigureAwait(false);
            if (order is not null)
            {
                foreach (var group in productGroups)
                {
                    var releasingQuantity = group.Quantity;
                    var reservedQuantity = await productReservationManager.GetReservedForOrderAsync(group.ProductId, order.Id).ConfigureAwait(false);
                    if (reservedQuantity < releasingQuantity)
                        throw new InvalidStockOperationException("Error.CannotReleaseMoreThanReserved", reservedQuantity, releasingQuantity);
                }

                foreach (var group in productGroups)
                {
                    await productReservationManager.ReleaseAsync(group.ProductId, group.Quantity, order.Id, ProductReservationReason.DeliveryNoteCreated, deliveryNote.Id)
                        .ConfigureAwait(false);
                }
            }
        }

        //reserve warehouse stock
        foreach (var group in productGroups)
        {
            await inventoryStockManager.ReserveStockAsync(
                group.ProductId,
                deliveryNote.WarehouseId,
                group.Quantity,
                deliveryNote.Id,
                Guid.Empty,
                $"Giữ hàng cho phiếu xuất {deliveryNote.Code}").ConfigureAwait(false);
        }
    }
}
