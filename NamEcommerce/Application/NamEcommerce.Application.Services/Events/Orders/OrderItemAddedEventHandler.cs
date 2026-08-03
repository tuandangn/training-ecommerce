using MediatR;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderItemAddedEventHandler(
    IProductReservationManager productReservationManager) : INotificationHandler<OrderItemAdded>
{
    public Task Handle(OrderItemAdded notification, CancellationToken cancellationToken)
        => productReservationManager.ReserveAsync(
            notification.ProductId,
            notification.Quantity,
            notification.OrderId,
            ProductReservationReason.OrderItemAdded,
            notification.OrderItemId);
}
