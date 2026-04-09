using MediatR;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderUpdatedEventHandler : INotificationHandler<EntityUpdatedNotification<Order>>
{
    private readonly IOrderManager _orderManager;

    public OrderUpdatedEventHandler(IOrderManager orderManager)
    {
        _orderManager = orderManager;
    }

    public async Task Handle(EntityUpdatedNotification<Order> notification, CancellationToken cancellationToken)
    {
        var order = notification.Entity;
        await _orderManager.VerifyStatusAsync(order.Id).ConfigureAwait(false);
    }
}
