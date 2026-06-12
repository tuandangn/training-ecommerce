using MediatR;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderCancelledHandler(IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
    IPurchaseOrderAllocationManager purchaseOrderAllocationManager) : INotificationHandler<PurchaseOrderCancelled>
{
    public async Task Handle(PurchaseOrderCancelled notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        var purchaseOrder = await purchaseOrderDataReader.GetByIdAsync(notification.PurchaseOrderId, default).ConfigureAwait(false);
        if (purchaseOrder is null)
            return;

        foreach (var purchaseOrderItem in purchaseOrder.Items)
        {
            await purchaseOrderAllocationManager.ReleaseAllocationsOfPurchaseOrderItemAsync((purchaseOrder.Id, purchaseOrderItem.Id)).ConfigureAwait(false);
        }
    }
}
