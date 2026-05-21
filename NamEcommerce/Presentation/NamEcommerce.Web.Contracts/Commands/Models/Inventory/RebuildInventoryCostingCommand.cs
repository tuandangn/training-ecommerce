using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Commands.Models.Inventory;

[Serializable]
public sealed class RebuildInventoryCostingCommand : IRequest<RebuildInventoryCostingResultModel>
{
    public required int CostingMethod { get; init; }
    public required int ValuationScope { get; init; }
}
