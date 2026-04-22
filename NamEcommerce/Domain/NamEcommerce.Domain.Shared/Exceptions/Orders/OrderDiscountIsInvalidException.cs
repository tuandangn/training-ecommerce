namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderDiscountIsInvalidException(string message)  : NamEcommerceDomainException("Error.OrderDiscountIsInvalid", message);


