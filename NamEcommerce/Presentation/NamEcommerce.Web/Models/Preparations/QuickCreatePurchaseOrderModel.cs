namespace NamEcommerce.Web.Models.Preparations;

public sealed class QuickCreatePurchaseOrderModel
{
    public Guid ProductId { get; set; }
    public Guid VendorId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Note { get; set; }
}
