namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.PurchaseOrderIsNotFound", id);


