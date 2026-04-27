namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrdersIdIsExistingException(Guid id) : NamEcommerceDomainException("Error.PurchaseOrders.IdExisting", id);
