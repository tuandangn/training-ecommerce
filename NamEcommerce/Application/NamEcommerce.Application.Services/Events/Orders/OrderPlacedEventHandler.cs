using MediatR;
using NamEcommerce.Domain.Shared.Events.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderCreatedEventHandler : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
