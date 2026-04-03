namespace NamEcommerce.Web.Models.Orders;

public sealed class CreateOrderModel
{
    public Guid? CustomerId { get; set; }
    public Guid? WarehouseId { get; set; }
    public decimal? OrderDiscount { get; set; }
    public string? Note { get; set; }
    public IList<CreateOrderItemModel> Items { get; set; } = new List<CreateOrderItemModel>();
}

public sealed class CreateOrderItemModel
{
    public Guid? ProductId { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}
