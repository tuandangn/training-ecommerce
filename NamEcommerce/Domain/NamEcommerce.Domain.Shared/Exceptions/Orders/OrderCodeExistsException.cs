namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderCodeExistsException(string code) : Exception($"Order with code '{code}' exists");

