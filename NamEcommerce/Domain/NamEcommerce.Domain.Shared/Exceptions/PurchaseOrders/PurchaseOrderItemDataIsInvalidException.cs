namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class PurchaseOrderItemDataIsInvalidException(string errorCode, params object[] parameters) : NamEcommerceDomainException(errorCode, parameters);


