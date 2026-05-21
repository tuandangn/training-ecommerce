using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record InventoryCostAllocation : AppAggregateEntity
{
    public InventoryCostAllocation(
        Guid id,
        Guid productId,
        Guid warehouseId,
        Guid outboundLedgerEntryId,
        InventoryCostReferenceType outboundReferenceType,
        Guid outboundReferenceId,
        Guid outboundReferenceItemId,
        Guid? inboundLayerId,
        decimal quantity,
        decimal? unitCost,
        decimal? totalCost,
        InventoryCostingStatus costingStatus,
        InventoryCostingMethod costingMethod,
        InventoryValuationScope valuationScope,
        Guid? costingRunId) : base(id)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        OutboundLedgerEntryId = outboundLedgerEntryId;
        OutboundReferenceType = outboundReferenceType;
        OutboundReferenceId = outboundReferenceId;
        OutboundReferenceItemId = outboundReferenceItemId;
        InboundLayerId = inboundLayerId;
        Quantity = quantity;
        UnitCost = unitCost;
        TotalCost = totalCost;
        CostingStatus = costingStatus;
        CostingMethod = costingMethod;
        ValuationScope = valuationScope;
        CostingRunId = costingRunId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid OutboundLedgerEntryId { get; private set; }
    public InventoryCostReferenceType OutboundReferenceType { get; private set; }
    public Guid OutboundReferenceId { get; private set; }
    public Guid OutboundReferenceItemId { get; private set; }
    public Guid? InboundLayerId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal? UnitCost { get; private set; }
    public decimal? TotalCost { get; private set; }
    public InventoryCostingStatus CostingStatus { get; private set; }
    public InventoryCostingMethod CostingMethod { get; private set; }
    public InventoryValuationScope ValuationScope { get; private set; }
    public Guid? CostingRunId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void MarkSuperseded(Guid costingRunId)
    {
        CostingStatus = InventoryCostingStatus.Superseded;
        CostingRunId = costingRunId;
    }
}
