namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class EditOrderShippingModel
{
    public Guid OrderId { get; set; }
    public DateTime? ExpectedShippingDate { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
}
