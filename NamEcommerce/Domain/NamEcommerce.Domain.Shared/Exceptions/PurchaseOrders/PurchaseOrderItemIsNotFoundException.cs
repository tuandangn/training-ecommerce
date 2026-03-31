namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderItemIsNotFoundException(Guid id) : Exception($"PurchaseOrderItem with id '{id}' is not found");
