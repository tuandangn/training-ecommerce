using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Commands.Models.Inventory;

[Serializable]
public sealed class UpdateInventoryCostingPolicyCommand : ICommand<UpdateInventoryCostingPolicyResultModel>
{
    public required int CostingMethod { get; init; }
    public required int ValuationScope { get; init; }
    public required DateTime EffectiveFrom { get; init; }
    public string? Note { get; init; }
}
