namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderIsAlreadyPaidException() : Exception("Order is already paid.");
