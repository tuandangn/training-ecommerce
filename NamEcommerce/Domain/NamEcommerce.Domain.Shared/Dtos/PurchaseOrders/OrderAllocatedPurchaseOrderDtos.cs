using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record OrderAllocatedPurchaseOrderDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public required PurchaseOrderStatus Status { get; init; }
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required DateTime PlacedOnUtc { get; init; }
    public DateTime? ExpectedDeliveryDateUtc { get; init; }
    public IList<OrderAllocatedPurchaseOrderItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record OrderAllocatedPurchaseOrderItemDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
}
