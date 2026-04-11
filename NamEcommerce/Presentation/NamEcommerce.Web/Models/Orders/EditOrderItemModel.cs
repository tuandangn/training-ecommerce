namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class EditOrderItemModel
{
    public Guid OrderId { get; set; }
    public Guid ItemId { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
