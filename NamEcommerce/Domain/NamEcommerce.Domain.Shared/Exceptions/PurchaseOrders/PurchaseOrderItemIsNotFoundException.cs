namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderItemIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.PurchaseOrderItemIsNotFoundException", id);

