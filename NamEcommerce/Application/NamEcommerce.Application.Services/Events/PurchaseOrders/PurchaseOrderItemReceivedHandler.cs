using MediatR;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderItemReceivedHandler(IPurchaseOrderManager purchaseOrderManager)
    : INotificationHandler<PurchaseOrderItemReceived>
{
    public async Task Handle(PurchaseOrderItemReceived notification, CancellationToken cancellationToken)
    {
        await purchaseOrderManager.VerifyStatusAsync(notification.PurchaseOrderId).ConfigureAwait(false);
    }
}
