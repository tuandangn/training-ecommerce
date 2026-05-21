using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record InventoryCostLayer : AppAggregateEntity
{
    public InventoryCostLayer(
        Guid id,
        Guid productId,
        Guid warehouseId,
        Guid sourceLedgerEntryId,
        InventoryCostReferenceType sourceReferenceType,
        Guid sourceReferenceId,
        Guid sourceReferenceItemId,
        DateTime openedAtUtc,
        decimal originalQuantity,
        decimal remainingQuantity,
        decimal? unitCost,
        decimal? totalCost,
        InventoryCostingStatus costingStatus,
        InventoryCostingMethod costingMethod,
        InventoryValuationScope valuationScope,
        Guid? costingRunId) : base(id)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        SourceLedgerEntryId = sourceLedgerEntryId;
        SourceReferenceType = sourceReferenceType;
        SourceReferenceId = sourceReferenceId;
        SourceReferenceItemId = sourceReferenceItemId;
        OpenedAtUtc = openedAtUtc;
        OriginalQuantity = originalQuantity;
        RemainingQuantity = remainingQuantity;
        UnitCost = unitCost;
        TotalCost = totalCost;
        CostingStatus = costingStatus;
        CostingMethod = costingMethod;
        ValuationScope = valuationScope;
        CostingRunId = costingRunId;
    }

    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid SourceLedgerEntryId { get; private set; }
    public InventoryCostReferenceType SourceReferenceType { get; private set; }
    public Guid SourceReferenceId { get; private set; }
    public Guid SourceReferenceItemId { get; private set; }
    public DateTime OpenedAtUtc { get; private set; }
    public decimal OriginalQuantity { get; private set; }
    public decimal RemainingQuantity { get; private set; }
    public decimal? UnitCost { get; private set; }
    public decimal? TotalCost { get; private set; }
    public InventoryCostingStatus CostingStatus { get; private set; }
    public InventoryCostingMethod CostingMethod { get; private set; }
    public InventoryValuationScope ValuationScope { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? CostingRunId { get; private set; }

    public void SetCost(decimal unitCost, InventoryCostingStatus status)
    {
        UnitCost = unitCost;
        TotalCost = OriginalQuantity * unitCost;
        CostingStatus = status;
    }

    public void Consume(decimal quantity)
    {
        RemainingQuantity -= quantity;
        if (RemainingQuantity <= 0)
        {
            RemainingQuantity = 0;
            ClosedAtUtc = DateTime.UtcNow;
        }
    }

    public void MarkSuperseded(Guid costingRunId)
    {
        CostingStatus = InventoryCostingStatus.Superseded;
        CostingRunId = costingRunId;
    }
}
