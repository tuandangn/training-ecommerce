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
    public required string Code { get; set; }
    public required Guid CustomerId { get; init; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhoneNumber { get; set; }
    public required decimal TotalAmount { get; init; }
    public decimal OrderDiscount { get; init; }
    public int Status { get; init; }
    public string? Note { get; init; }
    public int PaymentStatus { get; init; }
    public int? PaymentMethod { get; init; }
    public DateTime? PaidOnUtc { get; init; }
    public string? PaymentNote { get; init; }
    public int ShippingStatus { get; init; }
    public string? ShippingAddress { get; init; }
    public DateTime? ShippedOnUtc { get; init; }
    public string? ShippingNote { get; init; }
    public string? CancellationReason { get; init; }
    public IList<OrderDetailsItemModel> Items { get; init; } = new List<OrderDetailsItemModel>();
}
