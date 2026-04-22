namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderDataIsInvalidException(string? message)  : NamEcommerceDomainException("Error.OrderDataIsInvalidException", message);

