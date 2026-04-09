namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderCannotChangeStatusException() : Exception("Order cannot change status.");

