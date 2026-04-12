namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderLockedException() : Exception("Order is cancelled.");
