namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderCannotUpdateOrderItemsException() : Exception("Order cannot update items.");
