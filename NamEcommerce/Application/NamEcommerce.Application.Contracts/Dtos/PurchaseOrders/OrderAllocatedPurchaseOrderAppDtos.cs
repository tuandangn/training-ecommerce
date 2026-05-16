namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record OrderAllocatedPurchaseOrderAppDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public required int Status { get; init; }
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required DateTime PlacedOnUtc { get; init; }
    public DateTime? ExpectedDeliveryDateUtc { get; init; }
    public IList<OrderAllocatedPurchaseOrderItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record OrderAllocatedPurchaseOrderItemAppDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
}
