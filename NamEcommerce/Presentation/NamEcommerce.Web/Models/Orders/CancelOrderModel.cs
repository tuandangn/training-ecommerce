namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class CancelOrderModel
{
    public Guid OrderId { get; set; }
    public Guid? ReturnWarehouseId { get; set; }
}
