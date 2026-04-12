namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class LockOrderModel
{
    public Guid OrderId { get; set; }
    public string? Reason { get; set; }
}
