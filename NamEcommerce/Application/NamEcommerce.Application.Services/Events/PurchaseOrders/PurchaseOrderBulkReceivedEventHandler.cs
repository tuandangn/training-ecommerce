using MediatR;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderBulkReceivedEventHandler : INotificationHandler<PurchaseOrderBulkReceived>
{
    private readonly IPurchaseOrderManager _purchaseOrderManager;
    private readonly IPurchaseOrderAllocationManager _purchaseOrderAllocationManager;

    public PurchaseOrderBulkReceivedEventHandler(IPurchaseOrderManager purchaseOrderManager, IPurchaseOrderAllocationManager purchaseOrderAllocationManager)
    {
        _purchaseOrderManager = purchaseOrderManager;
        _purchaseOrderAllocationManager = purchaseOrderAllocationManager;
    }

    public async Task Handle(PurchaseOrderBulkReceived notification, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(notification.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is not null)
        {
            foreach (var item in purchaseOrder.Items.Where(item => item.QuantityReceived > 0))
            {
                await _purchaseOrderAllocationManager
                    .SyncReceivedForPurchaseOrderItemAsync(item.Id, item.QuantityReceived)
                    .ConfigureAwait(false);
            }
        }

        await _purchaseOrderManager.VerifyStatusAsync(notification.PurchaseOrderId).ConfigureAwait(false);
    }
}
