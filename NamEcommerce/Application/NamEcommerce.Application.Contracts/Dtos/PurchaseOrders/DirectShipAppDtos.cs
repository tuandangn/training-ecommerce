namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record PendingDirectShipDeliveryItemAppDto
{
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

[Serializable]
public sealed record PendingDirectShipDeliveryAppDto
{
    public required Guid Id { get; init; }
    public required Guid? WarehouseId { get; set; }
    public required string Code { get; init; }
    public required Guid OrderId { get; init; }
    public string? OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public required string ShippingAddress { get; init; }
    public string? ShippingPhoneNumber { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public required IList<PendingDirectShipDeliveryItemAppDto> Items { get; init; }
}

[Serializable]
public sealed record PendingDirectShipFilterAppDto
{
    public string? Keywords { get; init; }
    public DateTime? FromDateUtc { get; init; }
    public DateTime? ToDateUtc { get; init; }
}

[Serializable]
public sealed record MarkAllocationAsDirectShipAppDto
{
    public required Guid AllocationId { get; init; }
    public required string Address { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public int Priority { get; init; }
}

[Serializable]
public sealed record UpdateDirectShipAddressAppDto
{
    public required Guid AllocationId { get; init; }
    public required string NewAddress { get; init; }
    public string? NewContactName { get; init; }
    public string? NewContactPhone { get; init; }
    public string? Reason { get; init; }
}

[Serializable]
public sealed record ConfirmDirectShipDeliveryAppDto
{
    public required Guid DeliveryNoteId { get; init; }
    public required DateTime ConfirmedAtUtc { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed record RejectDirectShipDeliveryAppDto
{
    public required Guid DeliveryNoteId { get; init; }
    public required Guid ReturnWarehouseId { get; init; }
    public required string Reason { get; init; }
}
