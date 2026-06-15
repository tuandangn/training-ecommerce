using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderBulkReceivedHandler : INotificationHandler<PurchaseOrderBulkReceived>
{
    private readonly IPurchaseOrderManager _purchaseOrderManager;
    private readonly IOrderFulfillmentScheduleAppService _orderFulfillmentScheduleAppService;

    public PurchaseOrderBulkReceivedHandler(
        IPurchaseOrderManager purchaseOrderManager,
        IOrderFulfillmentScheduleAppService orderFulfillmentScheduleAppService)
    {
        _purchaseOrderManager = purchaseOrderManager;
        _orderFulfillmentScheduleAppService = orderFulfillmentScheduleAppService;
    }

    public async Task Handle(PurchaseOrderBulkReceived notification, CancellationToken cancellationToken)
    {
        await _purchaseOrderManager.VerifyStatusAsync(notification.PurchaseOrderId).ConfigureAwait(false);
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(notification.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return;

        await _orderFulfillmentScheduleAppService
            .RefreshWhenStockAvailableForPurchaseOrderItemsAsync(purchaseOrder.Items.Select(item => item.Id).ToList())
            .ConfigureAwait(false);
    }
}
