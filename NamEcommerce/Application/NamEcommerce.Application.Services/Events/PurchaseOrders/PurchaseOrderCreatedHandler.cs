using MediatR;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Application.Services.Notifications;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderCreatedHandler(IPurchaseOrderManager purchaseOrderManager)
    : INotificationHandler<PurchaseOrderCreated>
{
    public async Task Handle(PurchaseOrderCreated notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;
    }
}

public sealed class PurchaseOrderCreatedNotificationHandler(ISystemNotificationAppService notificationAppService)
    : INotificationHandler<PurchaseOrderCreated>
{
    public async Task Handle(PurchaseOrderCreated notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        await notificationAppService
            .CreateAsync(ProcurementSystemNotificationComposer.PurchaseOrderCreated(notification.PurchaseOrderId, notification.Code))
            .ConfigureAwait(false);
    }
}
