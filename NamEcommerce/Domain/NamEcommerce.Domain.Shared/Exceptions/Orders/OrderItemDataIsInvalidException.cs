namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderItemDataIsInvalidException(string? message)  : NamEcommerceDomainException("Error.OrderItemDataIsInvalidException", message);

