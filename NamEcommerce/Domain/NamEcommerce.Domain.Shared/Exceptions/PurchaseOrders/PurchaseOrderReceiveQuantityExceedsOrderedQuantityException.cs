namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderReceiveQuantityExceedsOrderedQuantityException() : Exception("PurchaseOrderItem received quantity exceeds ordered quantity.");
