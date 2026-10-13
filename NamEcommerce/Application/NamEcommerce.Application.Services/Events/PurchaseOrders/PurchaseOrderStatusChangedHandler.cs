using MediatR;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Application.Services.Notifications;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderStatusChangedHandler(
    IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
    ISystemNotificationAppService notificationAppService) : INotificationHandler<PurchaseOrderStatusChanged>
{
    public async Task Handle(PurchaseOrderStatusChanged notification, CancellationToken cancellationToken)
    {
        if (notification is null || !ProcurementSystemNotificationComposer.ShouldNotifyPurchaseOrderStatus(notification.NewStatus))
            return;

        var purchaseOrder = await purchaseOrderDataReader.GetByIdAsync(notification.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return;

        await notificationAppService
            .CreateAsync(ProcurementSystemNotificationComposer.PurchaseOrderStatusChanged(
                purchaseOrder.Id,
                purchaseOrder.Code,
                notification.OldStatus,
                notification.NewStatus))
            .ConfigureAwait(false);
    }
}
