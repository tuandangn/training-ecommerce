namespace NamEcommerce.Web.Contracts.Models.Orders;

[Serializable]
public sealed record OrderItemModel(Guid ProductId, decimal Quantity, decimal UnitPrice);
