namespace NamEcommerce.Application.Contracts.Dtos.Inventory;

[Serializable]
public sealed record InventoryCostingPolicyAppDto
{
    public required Guid Id { get; init; }
    public required int CostingMethod { get; init; }
    public required int ValuationScope { get; init; }
    public required DateTime EffectiveFromUtc { get; init; }
    public required bool IsActive { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed record UpdateInventoryCostingPolicyAppDto
{
    public required int CostingMethod { get; init; }
    public required int ValuationScope { get; init; }
    public required DateTime EffectiveFromUtc { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed record RebuildInventoryCostingAppDto
{
    public required int CostingMethod { get; init; }
    public required int ValuationScope { get; init; }
}
