namespace NamEcommerce.Domain.Shared.Exceptions.Debts;

[Serializable]
public sealed class VendorDebtAlreadyExistsForPurchaseOrderException(Guid purchaseOrderId)
     : NamEcommerceDomainException("Error.VendorDebtAlreadyExistsForPurchaseOrder", purchaseOrderId);


