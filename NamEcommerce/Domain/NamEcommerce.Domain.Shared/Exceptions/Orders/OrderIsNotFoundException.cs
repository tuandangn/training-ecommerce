namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderIsNotFoundException(Guid id) : Exception($"Order with id '{id}' is not found");
