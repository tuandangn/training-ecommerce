using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Domain.Shared.Enums.Notifications;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Application.Services.Notifications;

public static class ProcurementSystemNotificationComposer
{
    private const string GoodsReceiptRelatedEntityType = "GoodsReceipt";
    private const string PurchaseOrderRelatedEntityType = "PurchaseOrder";
    private const string GoodsReceiptsManagePermission = "GoodsReceipts.Manage";
    private const string PurchaseOrdersViewPermission = "PurchaseOrders.View";

    public static CreateSystemNotificationAppDto GoodsReceiptCreated(Guid goodsReceiptId, string code)
        => new()
        {
            Type = (int)SystemNotificationType.GoodsReceiptCreated,
            Severity = (int)SystemNotificationSeverity.Success,
            Title = $"Hàng vừa nhập kho {code}",
            Message = "Phiếu nhập kho mới vừa được tạo. Vui lòng kiểm tra hàng, giá vốn và chứng từ liên quan.",
            RequiredPermission = GoodsReceiptsManagePermission,
            RelatedEntityId = goodsReceiptId,
            RelatedEntityType = GoodsReceiptRelatedEntityType,
            ActionUrl = $"/GoodsReceipt/Details/{goodsReceiptId}"
        };

    public static CreateSystemNotificationAppDto PurchaseOrderCreated(Guid purchaseOrderId, string code)
        => new()
        {
            Type = (int)SystemNotificationType.PurchaseOrderCreated,
            Severity = (int)SystemNotificationSeverity.Warning,
            Title = $"Đơn nhập mới {code}",
            Message = "Đơn nhập mới vừa được tạo và cần theo dõi xử lý.",
            RequiredPermission = PurchaseOrdersViewPermission,
            RelatedEntityId = purchaseOrderId,
            RelatedEntityType = PurchaseOrderRelatedEntityType,
            ActionUrl = $"/PurchaseOrder/Details/{purchaseOrderId}"
        };

    public static CreateSystemNotificationAppDto PurchaseOrderStatusChanged(
        Guid purchaseOrderId,
        string code,
        PurchaseOrderStatus oldStatus,
        PurchaseOrderStatus newStatus)
        => new()
        {
            Type = (int)SystemNotificationType.PurchaseOrderStatusChanged,
            Severity = (int)SystemNotificationSeverity.Warning,
            Title = $"Đơn nhập {code} chuyển {newStatus}",
            Message = $"Trạng thái đơn nhập đổi từ {oldStatus} sang {newStatus}.",
            RequiredPermission = PurchaseOrdersViewPermission,
            RelatedEntityId = purchaseOrderId,
            RelatedEntityType = PurchaseOrderRelatedEntityType,
            ActionUrl = $"/PurchaseOrder/Details/{purchaseOrderId}"
        };

    public static bool ShouldNotifyPurchaseOrderStatus(PurchaseOrderStatus status)
        => status is PurchaseOrderStatus.Submitted or PurchaseOrderStatus.Approved or PurchaseOrderStatus.Receiving;
}
