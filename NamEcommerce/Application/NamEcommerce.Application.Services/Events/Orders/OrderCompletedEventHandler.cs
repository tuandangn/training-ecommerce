using MediatR;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderCompletedEventHandler(
    IProductReservationManager productReservationManager,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader) : INotificationHandler<OrderCompleted>
{
    public async Task Handle(OrderCompleted notification, CancellationToken cancellationToken)
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
            await productReservationManager.ReleaseAsync(
                item.ProductId,
                item.Quantity,
                order.Id,
                ProductReservationReason.OrderCompleted,
                order.Id).ConfigureAwait(false);
    }
}
