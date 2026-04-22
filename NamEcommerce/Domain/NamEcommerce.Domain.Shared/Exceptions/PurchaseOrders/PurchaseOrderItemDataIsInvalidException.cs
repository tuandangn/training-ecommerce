namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class PurchaseOrderItemDataIsInvalidException(string? message)  : NamEcommerceDomainException("Error.PurchaseOrderItemDataIsInvalidException", message);

