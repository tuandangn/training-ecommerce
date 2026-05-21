using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class RebuildInventoryCostingHandler : IRequestHandler<RebuildInventoryCostingCommand, RebuildInventoryCostingResultModel>
{
    private readonly IInventoryCostingAppService _inventoryCostingAppService;

    public RebuildInventoryCostingHandler(IInventoryCostingAppService inventoryCostingAppService)
    {
        _inventoryCostingAppService = inventoryCostingAppService;
    }

    public async Task<RebuildInventoryCostingResultModel> Handle(RebuildInventoryCostingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var runId = await _inventoryCostingAppService.RebuildAllAsync(new RebuildInventoryCostingAppDto
            {
                CostingMethod = request.CostingMethod,
                ValuationScope = request.ValuationScope
            }).ConfigureAwait(false);

            return new RebuildInventoryCostingResultModel
            {
                Success = true,
                RebuildRunId = runId
            };
        }
        catch (Exception ex)
        {
            return new RebuildInventoryCostingResultModel
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
