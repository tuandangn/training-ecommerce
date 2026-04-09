namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderDataIsInvalidException(string? message) : Exception(message);
