namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class PurchaseOrderDataIsInvalidException(string? message)  : NamEcommerceDomainException("Error.PurchaseOrderDataIsInvalidException", message);

