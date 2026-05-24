using MediatR;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderCreatedHandler : INotificationHandler<PurchaseOrderCreated>
{
    public Task Handle(PurchaseOrderCreated notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
