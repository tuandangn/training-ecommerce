namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderCannotUpdateOrderItemsException() : Exception("Purchase order cannot add items.");
