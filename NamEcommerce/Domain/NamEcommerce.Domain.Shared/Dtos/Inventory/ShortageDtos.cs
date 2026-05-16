namespace NamEcommerce.Domain.Shared.Dtos.Inventory;

[Serializable]
public sealed record PurchaseOrderShortageAllocationDto
{
    public required Guid POId { get; init; }
    public required string POCode { get; init; }
    public required decimal AllocatedQty { get; init; }
    public required decimal ReceivedQty { get; init; }
    public DateTime? ExpectedReceiveDateUtc { get; init; }
}

[Serializable]
public sealed record OrderItemShortageDto
{
    public required string OrderCode { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal RequiredQuantity { get; init; }
    public required decimal ShippedQuantity { get; init; }
    public required decimal AvailableQuantity { get; init; }
    public required decimal ShortageQuantity { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerAddress { get; init; }
    public IList<PurchaseOrderShortageAllocationDto> AllocatedFromPurchaseOrders { get; init; } = [];
}

[Serializable]
public sealed record DeliveryNoteItemShortageDto
{
    public required string OrderCode { get; init; }
    public required Guid DeliveryNoteItemId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal RequiredQuantity { get; init; }
    public required decimal ShippedQuantity { get; init; }
    public required decimal AvailableQuantity { get; init; }
    public required decimal ShortageQuantity { get; init; }
    public IList<PurchaseOrderShortageAllocationDto> AllocatedFromPurchaseOrders { get; init; } = [];
}

[Serializable]
public sealed record ShortageFilterDto
{
    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? ProductId { get; init; }
}
