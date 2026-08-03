using MediatR;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderDeletedEventHandler(
    IProductReservationManager productReservationManager) : INotificationHandler<OrderDeleted>
{
    public async Task Handle(OrderDeleted notification, CancellationToken cancellationToken)
    {
        foreach (var item in notification.Items)
            await productReservationManager.ReleaseAsync(
                item.ProductId,
                item.Quantity,
                notification.OrderId,
                ProductReservationReason.OrderDeleted,
                notification.OrderId).ConfigureAwait(false);
    }
}
