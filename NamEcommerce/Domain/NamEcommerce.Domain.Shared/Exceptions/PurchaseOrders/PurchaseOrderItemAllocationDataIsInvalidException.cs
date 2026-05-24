namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderItemAllocationDataIsInvalidException(string errorCode, params object[] parameters)
    : NamEcommerceDomainException(errorCode, parameters);
