using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderItemReceivedHandler(
    IPurchaseOrderManager purchaseOrderManager,
    IOrderFulfillmentScheduleAppService orderFulfillmentScheduleAppService)
    : INotificationHandler<PurchaseOrderItemReceived>
{
    public async Task Handle(PurchaseOrderItemReceived notification, CancellationToken cancellationToken)
    {
        await purchaseOrderManager.VerifyStatusAsync(notification.PurchaseOrderId).ConfigureAwait(false);
        await orderFulfillmentScheduleAppService
            .RefreshWhenStockAvailableForPurchaseOrderItemsAsync([(notification.PurchaseOrderId, notification.PurchaseOrderItemId)])
            .ConfigureAwait(false);
    }
}
