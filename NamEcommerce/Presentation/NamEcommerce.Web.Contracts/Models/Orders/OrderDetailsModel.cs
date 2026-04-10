namespace NamEcommerce.Web.Contracts.Models.Orders;

[Serializable]
public sealed record OrderDetailsModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required decimal TotalAmount { get; init; }
    public required Guid CustomerId { get; init; }

    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhoneNumber { get; set; }

    public decimal OrderDiscount { get; set; }
    public int Status { get; set; }
    public string? CancellationReason { get; set; }

    public string? Note { get; set; }

    public int PaymentStatus { get; set; }
    public int? PaymentMethod { get; set; }
    public DateTime? PaidOnUtc { get; set; }
    public string? PaymentNote { get; set; }

    public int ShippingStatus { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime? ShippedOnUtc { get; set; }
    public string? ShippingNote { get; set; }

    public bool CanUpdateInfo { get; set; }
    public bool CanUpdateOrderItems { get; set; }

    public IList<OrderDetailsItemModel> Items { get; init; } = [];
}

[Serializable]
public sealed record OrderDetailsItemModel(Guid Id)
{
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public string? ProductName { get; set; }
    public string? ProductPicture { get; set; }
    public decimal? ProductAvailableQty { get; set; }
    public decimal SubTotal => UnitPrice * Quantity;
}
