namespace NamEcommerce.Domain.Shared.Exceptions.Debts;

[Serializable]
public sealed class VendorDebtAlreadyExistsForPurchaseOrderException(Guid purchaseOrderId)
    : Exception($"VendorDebt already exists for PurchaseOrder with id '{purchaseOrderId}'");
