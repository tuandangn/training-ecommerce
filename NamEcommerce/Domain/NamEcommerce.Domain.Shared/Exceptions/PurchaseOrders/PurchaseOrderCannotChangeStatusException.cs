namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderCannotChangeStatusException() : Exception("PurchaseOrder cannot change status.");

