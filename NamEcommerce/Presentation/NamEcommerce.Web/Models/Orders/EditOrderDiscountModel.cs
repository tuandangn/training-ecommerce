namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class EditOrderDiscountModel
{
    public Guid OrderId { get; set; }
    public decimal? OrderDiscount { get; set; }
}
