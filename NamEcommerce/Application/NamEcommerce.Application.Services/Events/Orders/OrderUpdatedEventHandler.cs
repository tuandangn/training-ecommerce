using MediatR;
using NamEcommerce.Domain.Entities.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderUpdatedEventHandler : INotificationHandler<EntityUpdatedNotification<Order>>
{
    public Task Handle(EntityUpdatedNotification<Order> notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
