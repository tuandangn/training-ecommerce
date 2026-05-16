using MediatR;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

/// <summary>
/// Handler cho <see cref="PurchaseOrderItemReceived"/>.
/// Khi một dòng hàng vừa được nhận hàng, gọi <see cref="IPurchaseOrderManager.VerifyStatusAsync"/>
/// để tự transition trạng thái đơn (Approved → Receiving → Completed) tuỳ theo mức độ đã nhận.
///
/// <para>Nếu <see cref="IDirectShipManager"/> được inject, việc phân bổ allocation đã được thực hiện
/// inline trong <see cref="IPurchaseOrderManager"/> — handler chỉ verify status, bỏ qua Sync.</para>
/// </summary>
public sealed class PurchaseOrderItemReceivedEventHandler(
    IPurchaseOrderManager purchaseOrderManager,
    IPurchaseOrderAllocationManager purchaseOrderAllocationManager,
    IDirectShipManager? directShipManager = null) : INotificationHandler<PurchaseOrderItemReceived>
{
    public async Task Handle(PurchaseOrderItemReceived notification, CancellationToken cancellationToken)
    {
        // Khi direct-ship được kích hoạt, PurchaseOrderManager đã phân bổ allocation inline.
        // Khi không có direct-ship, event handler phân bổ FIFO như cũ.
        if (directShipManager == null)
        {
            var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(notification.PurchaseOrderId).ConfigureAwait(false);
            var item = purchaseOrder?.Items.FirstOrDefault(i => i.Id == notification.PurchaseOrderItemId);
            if (item is not null)
            {
                await purchaseOrderAllocationManager
                    .SyncReceivedForPurchaseOrderItemAsync(item.Id, item.QuantityReceived)
                    .ConfigureAwait(false);
            }
        }

        await purchaseOrderManager.VerifyStatusAsync(notification.PurchaseOrderId).ConfigureAwait(false);
    }
}
