namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderVendorIsNotAppropriateException() : NamEcommerceDomainException("Error.PurchaseOrder.VendorIsNotAppropriate");
