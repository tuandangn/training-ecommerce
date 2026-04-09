namespace NamEcommerce.Web.Contracts.Models.Orders;

[Serializable]
public sealed record OrderItemModel
{
    public required Guid ProductId { get; set; }
    public required decimal Quantity { get; set; }
    public required decimal UnitPrice { get; set; }
}
