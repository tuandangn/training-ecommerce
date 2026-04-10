namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderIsAlreadyShippedException() : Exception("Order is already shipped.");
