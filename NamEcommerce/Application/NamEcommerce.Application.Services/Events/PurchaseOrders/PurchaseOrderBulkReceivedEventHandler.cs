using MediatR;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderBulkReceivedEventHandler : INotificationHandler<PurchaseOrderBulkReceived>
{
    private readonly IPurchaseOrderManager _purchaseOrderManager;

    public PurchaseOrderBulkReceivedEventHandler(IPurchaseOrderManager purchaseOrderManager)
    {
        _purchaseOrderManager = purchaseOrderManager;
    }

    public async Task Handle(PurchaseOrderBulkReceived notification, CancellationToken cancellationToken)
    {
        await _purchaseOrderManager.VerifyStatusAsync(notification.PurchaseOrderId).ConfigureAwait(false);
    }
}
