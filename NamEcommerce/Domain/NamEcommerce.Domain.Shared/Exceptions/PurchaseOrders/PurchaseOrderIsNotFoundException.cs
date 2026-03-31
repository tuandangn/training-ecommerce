namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderIsNotFoundException(Guid id) : Exception($"PurchaseOrder with id '{id}' is not found");
