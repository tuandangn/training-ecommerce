namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderItemAllocationIsNotFoundException(Guid id)
    : NamEcommerceDomainException("Error.PurchaseOrderItemAllocationIsNotFound", id);
