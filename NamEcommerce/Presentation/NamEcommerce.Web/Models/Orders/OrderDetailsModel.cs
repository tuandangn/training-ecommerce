namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed record OrderDetailsModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required decimal OrderSubTotal { get; init; }
    public required decimal TotalAmount { get; init; }
    public required Guid CustomerId { get; init; }

    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhoneNumber { get; set; }

    public decimal OrderDiscount { get; set; }
    public int Status { get; set; }
    public string? LockOrderReason { get; set; }

    public string? Note { get; set; }

    public DateTime? ExpectedShippingDate { get; set; }
    public string? ShippingAddress { get; set; }

    public bool CanUpdateInfo { get; init; }
    public bool CanLockOrder { get; init; }
    public bool CanUpdateOrderItems { get; init; }

    public IList<OrderItemModel> Items { get; init; } = [];

    [Serializable]
    public sealed record OrderItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public required decimal Quantity { get; init; }
        public required decimal UnitPrice { get; init; }
        public string? ProductName { get; set; }
        public string? ProductPicture { get; set; }
        public decimal? ProductAvailableQty { get; set; }
        public decimal SubTotal => UnitPrice * Quantity;
    }
}
