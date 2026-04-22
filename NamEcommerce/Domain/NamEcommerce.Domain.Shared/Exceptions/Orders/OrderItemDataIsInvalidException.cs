namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderItemDataIsInvalidException(string errorCode, params object[] parameters) : NamEcommerceDomainException(errorCode, parameters);


