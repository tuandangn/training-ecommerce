namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class AddOrderItemModel
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
