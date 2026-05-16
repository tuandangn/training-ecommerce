using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrderItemAllocationDto(Guid Id)
{
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required AllocationStatus Status { get; init; }
    public bool IsDirectShip { get; init; }
    public string? DirectShipAddress { get; init; }
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
    public int DirectShipPriority { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record AllocationReceiptDto
{
    public required Guid AllocationId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required bool IsDirectShip { get; init; }
    public string? DirectShipAddress { get; init; }
}

[Serializable]
public sealed record DistributeReceivedQuantityResultDto
{
    public required IReadOnlyList<AllocationReceiptDto> DirectShipReceipts { get; init; }
    public required IReadOnlyList<AllocationReceiptDto> WarehouseReceipts { get; init; }
}
