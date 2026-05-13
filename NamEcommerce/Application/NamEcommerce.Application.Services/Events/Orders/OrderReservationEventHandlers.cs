using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderItemAddedEventHandler(
    IProductReservationManager productReservationManager) : INotificationHandler<OrderItemAdded>
{
    public Task Handle(OrderItemAdded notification, CancellationToken cancellationToken)
        => productReservationManager.ReserveAsync(notification.ProductId, notification.Quantity, notification.OrderId);
}

public sealed class OrderItemUpdatedEventHandler(
    IProductReservationManager productReservationManager) : INotificationHandler<OrderItemUpdated>
{
    public Task Handle(OrderItemUpdated notification, CancellationToken cancellationToken)
    {
        var deltaQuantity = notification.Quantity - notification.OldQuantity;
        return productReservationManager.AdjustAsync(notification.ProductId, deltaQuantity, notification.OrderId);
    }
}

public sealed class OrderItemRemovedEventHandler(
    IProductReservationManager productReservationManager) : INotificationHandler<OrderItemRemoved>
{
    public Task Handle(OrderItemRemoved notification, CancellationToken cancellationToken)
        => productReservationManager.ReleaseAsync(notification.ProductId, notification.Quantity, notification.OrderId);
}

public sealed class OrderDeletedEventHandler(
    IProductReservationManager productReservationManager) : INotificationHandler<OrderDeleted>
{
    public async Task Handle(OrderDeleted notification, CancellationToken cancellationToken)
    {
        foreach (var item in notification.Items)
            await productReservationManager.ReleaseAsync(item.ProductId, item.Quantity, notification.OrderId).ConfigureAwait(false);
    }
}

public sealed class OrderCancelledEventHandler(
    IProductReservationManager productReservationManager) : INotificationHandler<OrderCancelled>
{
    public async Task Handle(OrderCancelled notification, CancellationToken cancellationToken)
    {
        foreach (var item in notification.Items)
            await productReservationManager.ReleaseAsync(item.ProductId, item.Quantity, notification.OrderId).ConfigureAwait(false);
    }
}

public sealed class OrderLockedEventHandler(
    IProductReservationManager productReservationManager,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader) : INotificationHandler<OrderLocked>
{
    public async Task Handle(OrderLocked notification, CancellationToken cancellationToken)
    {
        var order = await orderReader.GetByIdAsync(notification.OrderId).ConfigureAwait(false);
        if (order is null) return;

        var movedQuantities = deliveryNoteReader.DataSource
            .Where(note => note.OrderId == order.Id
                && note.Status != DeliveryNoteStatus.Draft
                && note.Status != DeliveryNoteStatus.Cancelled)
            .SelectMany(note => note.Items)
            .GroupBy(item => item.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(item => item.Quantity));

        var remainingGlobalQuantities = order.OrderItems
            .GroupBy(item => item.ProductId)
            .Select(g =>
            {
                var movedQuantity = movedQuantities.GetValueOrDefault(g.Key);
                return new OrderReservationItem(g.Key, Math.Max(0, g.Sum(item => item.Quantity) - movedQuantity));
            })
            .Where(item => item.Quantity > 0)
            .ToList();

        foreach (var item in remainingGlobalQuantities)
            await productReservationManager.ReleaseAsync(item.ProductId, item.Quantity, order.Id).ConfigureAwait(false);
    }
}
