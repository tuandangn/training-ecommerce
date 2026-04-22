namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderCodeExistsException(string code)  : NamEcommerceDomainException("Error.PurchaseOrderCodeExists", code);



