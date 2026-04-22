namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderItemIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.OrderItemIsNotFoundException", id);

