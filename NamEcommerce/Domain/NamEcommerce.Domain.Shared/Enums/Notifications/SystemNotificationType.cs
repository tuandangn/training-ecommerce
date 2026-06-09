namespace NamEcommerce.Domain.Shared.Enums.Notifications;

public enum SystemNotificationType
{
    CustomerPortalOrderRequestCreated = 10,
    CustomerPortalReturnRequestCreated = 20,
    CustomerPortalDeliveryConfirmed = 30,
    CustomerPortalPaymentPendingReconciliation = 40,
    DeliveryNoteCreated = 100,
    DeliveryNoteConfirmed = 110,
    DeliveryNoteDelivering = 120,
    DeliveryNoteDelivered = 130,
    DeliveryRunCreated = 200,
    DeliveryRunHandedOver = 210,
    DeliveryRunCashHandoverPending = 220,
    GoodsReceiptCreated = 300,
    PurchaseOrderCreated = 400,
    PurchaseOrderStatusChanged = 410,
    InventoryCostReturnReversalLost = 500
}
