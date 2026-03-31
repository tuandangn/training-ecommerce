namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderCodeExistsException(string code) : Exception($"PurchaseOrder with code '{code}' exists");

