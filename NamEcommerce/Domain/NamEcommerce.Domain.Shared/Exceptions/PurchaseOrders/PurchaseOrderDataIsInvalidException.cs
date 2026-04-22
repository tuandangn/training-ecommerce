namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class PurchaseOrderDataIsInvalidException(string errorCode, params object[] parameters) : NamEcommerceDomainException(errorCode, parameters);


