using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record InventoryCostingPolicy : AppAggregateEntity
{
    public InventoryCostingPolicy(Guid id, InventoryCostingMethod costingMethod, InventoryValuationScope valuationScope, DateTime effectiveFromUtc, Guid? createdByUserId, string? note) : base(id)
    {
        CostingMethod = costingMethod;
        ValuationScope = valuationScope;
        EffectiveFromUtc = effectiveFromUtc;
        IsActive = true;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTime.UtcNow;
        Note = note;
    }

    public InventoryCostingMethod CostingMethod { get; private set; }
    public InventoryValuationScope ValuationScope { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? Note { get; private set; }

    public void Deactivate()
        => IsActive = false;
}
