using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record InventoryCostRebuildRun : AppAggregateEntity
{
    public InventoryCostRebuildRun(
        Guid id,
        InventoryCostRebuildTrigger trigger,
        InventoryCostingMethod costingMethod,
        InventoryValuationScope valuationScope,
        DateTime fromUtc,
        DateTime? toUtc,
        Guid? productId,
        Guid? requestedByUserId) : base(id)
    {
        Status = InventoryCostRebuildStatus.Running;
        Trigger = trigger;
        CostingMethod = costingMethod;
        ValuationScope = valuationScope;
        FromUtc = fromUtc;
        ToUtc = toUtc;
        ProductId = productId;
        RequestedByUserId = requestedByUserId;
        StartedAtUtc = DateTime.UtcNow;
    }

    public InventoryCostRebuildStatus Status { get; private set; }
    public InventoryCostRebuildTrigger Trigger { get; private set; }
    public InventoryCostingMethod CostingMethod { get; private set; }
    public InventoryValuationScope ValuationScope { get; private set; }
    public DateTime FromUtc { get; private set; }
    public DateTime? ToUtc { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void Complete()
    {
        Status = InventoryCostRebuildStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void Fail(string errorMessage)
    {
        Status = InventoryCostRebuildStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }
}
