namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderItemDataIsInvalidException(string? message) : Exception(message);
