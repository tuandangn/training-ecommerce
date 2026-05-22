using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class UpdateInventoryCostingPolicyHandler : IRequestHandler<UpdateInventoryCostingPolicyCommand, UpdateInventoryCostingPolicyResultModel>
{
    private readonly IInventoryCostingAppService _inventoryCostingAppService;

    public UpdateInventoryCostingPolicyHandler(IInventoryCostingAppService inventoryCostingAppService)
    {
        _inventoryCostingAppService = inventoryCostingAppService;
    }

    public async Task<UpdateInventoryCostingPolicyResultModel> Handle(UpdateInventoryCostingPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await _inventoryCostingAppService.UpdatePolicyAsync(new UpdateInventoryCostingPolicyAppDto
        {
            CostingMethod = request.CostingMethod,
            ValuationScope = request.ValuationScope,
            EffectiveFromUtc = DateTimeHelper.ToUniversalTime(request.EffectiveFrom),
            Note = request.Note
        }).ConfigureAwait(false);

        return new UpdateInventoryCostingPolicyResultModel
        {
            Success = true,
            UpdatedId = policy.Id
        };
    }
}
