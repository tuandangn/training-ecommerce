using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record InventoryCostLedgerEntry : AppAggregateEntity
{
    public InventoryCostLedgerEntry(
        Guid id,
        Guid productId,
        Guid warehouseId,
        DateTime occurredAtUtc,
        long sequenceNumber,
        InventoryCostMovementType movementType,
        decimal quantityDelta,
        decimal? unitCost,
        decimal? totalCost,
        decimal quantityBalanceAfter,
        decimal valueBalanceAfter,
        decimal averageCostAfter,
        InventoryCostingStatus costingStatus,
        InventoryCostingMethod costingMethod,
        InventoryValuationScope valuationScope,
        InventoryCostReferenceType referenceType,
        Guid referenceId,
        Guid referenceItemId,
        Guid? costingRunId) : base(id)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        OccurredAtUtc = occurredAtUtc;
        SequenceNumber = sequenceNumber;
        MovementType = movementType;
        QuantityDelta = quantityDelta;
        UnitCost = unitCost;
        TotalCost = totalCost;
        QuantityBalanceAfter = quantityBalanceAfter;
        ValueBalanceAfter = valueBalanceAfter;
        AverageCostAfter = averageCostAfter;
        CostingStatus = costingStatus;
        CostingMethod = costingMethod;
        ValuationScope = valuationScope;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        ReferenceItemId = referenceItemId;
        CostingRunId = costingRunId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public long SequenceNumber { get; private set; }
    public InventoryCostMovementType MovementType { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public decimal? UnitCost { get; private set; }
    public decimal? TotalCost { get; private set; }
    public decimal QuantityBalanceAfter { get; private set; }
    public decimal ValueBalanceAfter { get; private set; }
    public decimal AverageCostAfter { get; private set; }
    public InventoryCostingStatus CostingStatus { get; private set; }
    public InventoryCostingMethod CostingMethod { get; private set; }
    public InventoryValuationScope ValuationScope { get; private set; }
    public InventoryCostReferenceType ReferenceType { get; private set; }
    public Guid ReferenceId { get; private set; }
    public Guid ReferenceItemId { get; private set; }
    public Guid? CostingRunId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void MarkSuperseded(Guid costingRunId)
    {
        CostingStatus = InventoryCostingStatus.Superseded;
        CostingRunId = costingRunId;
    }
}
