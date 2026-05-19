namespace NamEcommerce.Web.Models.PurchaseOrders;

public sealed class AllocateToOrderRequestModel
{
    public Guid PurchaseOrderItemId { get; set; }
    public Guid OrderId { get; set; }
    public Guid OrderItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? DirectShipAddress { get; set; }
    public string? DirectShipContactName { get; set; }
    public string? DirectShipContactPhone { get; set; }
}
