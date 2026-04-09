namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderItemIsNotFoundException(Guid id) : Exception($"Order item with id '{id}' is not found");
