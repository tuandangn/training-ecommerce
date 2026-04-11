namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class MarkOrderAsPaidModel
{
    public Guid OrderId { get; set; }
    public int? PaymentMethod { get; set; }
    public string? Note { get; set; }
}
