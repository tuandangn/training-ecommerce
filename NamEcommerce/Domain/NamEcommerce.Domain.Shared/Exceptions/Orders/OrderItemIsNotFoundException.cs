namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderItemIsNotFoundException() : NamEcommerceDomainException("Error.OrderItemIsNotFound");


