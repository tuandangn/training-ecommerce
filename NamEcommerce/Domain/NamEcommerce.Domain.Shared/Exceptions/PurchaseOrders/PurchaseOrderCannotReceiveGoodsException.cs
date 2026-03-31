namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderCannotReceiveGoodsException() : Exception("PurchaseOrder cannot receive goods.");

