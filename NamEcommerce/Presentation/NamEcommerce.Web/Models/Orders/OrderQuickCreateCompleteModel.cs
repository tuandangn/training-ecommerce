namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class OrderQuickCreateCompleteModel
{
    public Guid OrderId { get; set; }
    public decimal PaidAmount { get; set; }
    public Guid? PaymentIntentId { get; set; }
}
