using MediatR;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

/// <summary>
/// Handler cho <see cref="PurchaseOrderItemReceived"/>.
/// Khi một dòng hàng vừa được nhận hàng, gọi <see cref="IPurchaseOrderManager.VerifyStatusAsync"/>
/// để tự transition trạng thái đơn (Approved → Receiving → Completed) tuỳ theo mức độ đã nhận.
///
/// <para>Trước đây handler này lắng nghe <c>EntityUpdatedNotification&lt;PurchaseOrder&gt;</c> nên
/// chạy trên MỌI lần update đơn (kể cả update note, vendor, warehouse...) — tốn nhiều round trip
/// vô nghĩa. Sau khi migrate sang concrete event, handler chỉ chạy đúng khi có item được nhận.</para>
/// </summary>
public sealed class PurchaseOrderItemReceivedEventHandler : INotificationHandler<PurchaseOrderItemReceived>
{
    private readonly IPurchaseOrderManager _purchaseOrderManager;
    private readonly IPurchaseOrderAllocationManager _purchaseOrderAllocationManager;

    public PurchaseOrderItemReceivedEventHandler(IPurchaseOrderManager purchaseOrderManager, IPurchaseOrderAllocationManager purchaseOrderAllocationManager)
    {
        _purchaseOrderManager = purchaseOrderManager;
        _purchaseOrderAllocationManager = purchaseOrderAllocationManager;
    }

    public async Task Handle(PurchaseOrderItemReceived notification, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(notification.PurchaseOrderId).ConfigureAwait(false);
        var item = purchaseOrder?.Items.FirstOrDefault(item => item.Id == notification.PurchaseOrderItemId);
        if (item is not null)
        {
            await _purchaseOrderAllocationManager
                .SyncReceivedForPurchaseOrderItemAsync(item.Id, item.QuantityReceived)
                .ConfigureAwait(false);
        }

        await _purchaseOrderManager.VerifyStatusAsync(notification.PurchaseOrderId).ConfigureAwait(false);
    }
}
