namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class DirectShipDataIsInvalidException(string errorCode, params object[] parameters)
    : NamEcommerceDomainException(errorCode, parameters);
