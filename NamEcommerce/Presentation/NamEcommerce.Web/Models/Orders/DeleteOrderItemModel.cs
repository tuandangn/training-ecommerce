namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class DeleteOrderItemModel
{
    public Guid OrderId { get; set; }
    public Guid ItemId { get; set; }
}
