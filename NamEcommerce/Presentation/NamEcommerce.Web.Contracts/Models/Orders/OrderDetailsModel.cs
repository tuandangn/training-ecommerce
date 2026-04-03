namespace NamEcommerce.Web.Contracts.Models.Orders;

[Serializable]
public sealed record OrderDetailsItemModel
{
    public required Guid ItemId { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; set; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

[Serializable]
public sealed record OrderDetailsModel
{
    public required Guid Id { get; init; }
    public required Guid CustomerId { get; init; }
    public string? CustomerName { get; set; }
    public required decimal TotalAmount { get; init; }
    public decimal OrderDiscount { get; init; }
    public int Status { get; init; }
    public string? Note { get; init; }
    public IList<OrderDetailsItemModel> Items { get; init; } = new List<OrderDetailsItemModel>();
}
