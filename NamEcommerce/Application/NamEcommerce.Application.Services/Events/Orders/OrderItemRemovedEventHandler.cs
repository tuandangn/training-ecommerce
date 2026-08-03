using MediatR;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderItemRemovedEventHandler(
    IProductReservationManager productReservationManager) : INotificationHandler<OrderItemRemoved>
{
    public Task Handle(OrderItemRemoved notification, CancellationToken cancellationToken)
        => productReservationManager.ReleaseAsync(
            notification.ProductId,
            notification.Quantity,
            notification.OrderId,
            ProductReservationReason.OrderItemRemoved,
            notification.OrderItemId);
}
