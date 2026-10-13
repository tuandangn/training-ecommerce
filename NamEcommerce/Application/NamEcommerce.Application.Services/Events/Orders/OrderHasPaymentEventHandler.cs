using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderHasPaymentEventHandler(IOrderManager orderManager, IQuickCreateOrderAppService quickCreateOrderAppService)
    : INotificationHandler<OrderHasPayment>
{
    public async Task Handle(OrderHasPayment notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var order = await orderManager.GetOrderByIdAsync(notification.OrderId).ConfigureAwait(false);

        if (order is null)
            return;

        if (!order.ProcessRequiresPayment)
            return;

        if (!order.CanProcess)
            return;

        await quickCreateOrderAppService.ProcessOrderAsync(order.Id).ConfigureAwait(false);
    }
}
