namespace NamEcommerce.Domain.Shared.Exceptions.Orders;

[Serializable]
public sealed class OrderCannotUpdateInfoException() : Exception("Order cannot update info.");
