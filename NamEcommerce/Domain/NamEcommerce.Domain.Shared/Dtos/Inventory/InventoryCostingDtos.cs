using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.Inventory;

[Serializable]
public sealed record RegisterInventoryInboundCostDto
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal? UnitCost { get; init; }
    public required InventoryCostMovementType MovementType { get; init; }
    public required InventoryCostReferenceType ReferenceType { get; init; }
    public required Guid ReferenceId { get; init; }
    public required Guid ReferenceItemId { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
}

[Serializable]
public sealed record RegisterInventoryOutboundCostDto
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
    public required InventoryCostMovementType MovementType { get; init; }
    public required InventoryCostReferenceType ReferenceType { get; init; }
    public required Guid ReferenceId { get; init; }
    public required Guid ReferenceItemId { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
}

[Serializable]
public sealed record RegisterInventoryReceiptReversalCostDto
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
    public required Guid GoodsReceiptId { get; init; }
    public required Guid GoodsReceiptItemId { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
}

[Serializable]
public sealed record RegisterInventoryTransferInCostDto
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitCost { get; init; }
    public required InventoryCostingStatus SourceStatus { get; init; }
    public required InventoryCostReferenceType ReferenceType { get; init; }
    public required Guid ReferenceId { get; init; }
    public required Guid ReferenceItemId { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
}

[Serializable]
public sealed record AssignGoodsReceiptItemCostDto
{
    public required Guid GoodsReceiptId { get; init; }
    public required Guid GoodsReceiptItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal UnitCost { get; init; }
}

[Serializable]
public sealed record InventoryCostSummaryDto
{
    public required Guid ProductId { get; init; }
    public required decimal QuantityBalance { get; init; }
    public required decimal ValueBalance { get; init; }
    public required decimal AverageCost { get; init; }
    public required InventoryCostingStatus Status { get; init; }
}

[Serializable]
public sealed record InventoryCostMovementResultDto
{
    public required Guid LedgerEntryId { get; init; }
    public required decimal UnitCost { get; init; }
    public required decimal TotalCost { get; init; }
    public required InventoryCostingStatus Status { get; init; }
}

[Serializable]
public sealed record RegisterSaleDispatchReversalDto
{
    public required Guid ProductId { get; init; }
    public required decimal ReturnedQuantity { get; init; }
    public required Guid DeliveryNoteItemId { get; init; }
    public required Guid InboundLedgerEntryId { get; init; }
}

[Serializable]
public sealed record InventoryCogsSummaryDto
{
    public required decimal TotalCost { get; init; }
    public required bool HasPendingCost { get; init; }
    public required bool HasRevaluedCost { get; init; }
}
