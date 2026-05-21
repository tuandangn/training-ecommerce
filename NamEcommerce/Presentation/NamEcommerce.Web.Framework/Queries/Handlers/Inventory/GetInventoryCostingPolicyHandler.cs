using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetInventoryCostingPolicyHandler : IRequestHandler<GetInventoryCostingPolicyQuery, InventoryCostingPolicySettingsModel>
{
    private readonly IInventoryCostingAppService _inventoryCostingAppService;

    public GetInventoryCostingPolicyHandler(IInventoryCostingAppService inventoryCostingAppService)
    {
        _inventoryCostingAppService = inventoryCostingAppService;
    }

    public async Task<InventoryCostingPolicySettingsModel> Handle(GetInventoryCostingPolicyQuery request, CancellationToken cancellationToken)
    {
        var policy = await _inventoryCostingAppService.GetActivePolicyAsync().ConfigureAwait(false);

        return new InventoryCostingPolicySettingsModel
        {
            Id = policy.Id,
            CostingMethod = policy.CostingMethod,
            ValuationScope = policy.ValuationScope,
            EffectiveFrom = DateTimeHelper.ToLocalTime(policy.EffectiveFromUtc),
            CreatedAt = DateTimeHelper.ToLocalTime(policy.CreatedAtUtc),
            Note = policy.Note
        };
    }
}
