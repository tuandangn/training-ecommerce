using MediatR;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderUpdatedEventHandler : INotificationHandler<EntityUpdatedNotification<PurchaseOrder>>
{
    private readonly IPurchaseOrderManager _purchaseOrderManager;

    public PurchaseOrderUpdatedEventHandler(IPurchaseOrderManager purchaseOrderManager)
    {
        _purchaseOrderManager = purchaseOrderManager;
    }

    public async Task Handle(EntityUpdatedNotification<PurchaseOrder> notification, CancellationToken cancellationToken)
    {
        var purchaseOrder = notification.Entity;
        await _purchaseOrderManager.VerifyStatusAsync(purchaseOrder.Id).ConfigureAwait(false);
    }
}
