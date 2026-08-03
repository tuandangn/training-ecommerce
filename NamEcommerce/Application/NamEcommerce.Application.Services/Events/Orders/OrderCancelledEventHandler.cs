using MediatR;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderCancelledEventHandler(
    IProductReservationManager productReservationManager) : INotificationHandler<OrderCancelled>
{
    public async Task Handle(OrderCancelled notification, CancellationToken cancellationToken)
    {
        foreach (var item in notification.Items)
            await productReservationManager.ReleaseAsync(
                item.ProductId,
                item.Quantity,
                notification.OrderId,
                ProductReservationReason.OrderCancelled,
                notification.OrderId).ConfigureAwait(false);
    }
}
