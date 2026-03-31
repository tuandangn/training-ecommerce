namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderCannotAddItemException() : Exception("PurchaseOrder cannot add items.");

