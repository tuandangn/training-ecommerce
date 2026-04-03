namespace NamEcommerce.Web.Models.Orders;

public sealed class OrderDetailsModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal OrderDiscount { get; set; }
    public int Status { get; set; }
    public string? Note { get; set; }
    public IList<OrderDetailsItemModel> Items { get; set; } = new List<OrderDetailsItemModel>();
}

public sealed class OrderDetailsItemModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
