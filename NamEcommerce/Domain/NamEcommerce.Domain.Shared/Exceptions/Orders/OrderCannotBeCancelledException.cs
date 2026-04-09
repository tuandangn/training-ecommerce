namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderCannotBeCancelledException() : Exception("Order cannot be cancelled.");
