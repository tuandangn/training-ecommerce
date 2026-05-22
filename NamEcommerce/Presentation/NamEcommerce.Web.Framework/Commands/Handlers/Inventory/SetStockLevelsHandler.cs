using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class SetStockLevelsHandler : IRequestHandler<SetStockLevelsCommand, SetStockLevelsResultModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public SetStockLevelsHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<SetStockLevelsResultModel> Handle(SetStockLevelsCommand request, CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.SetStockLevelsAsync(new SetStockLevelsAppDto(request.Id)
        {
            ReorderLevel = request.ReorderLevel,
            MaxStockLevel = request.MaxStockLevel
        }).ConfigureAwait(false);

        return new SetStockLevelsResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            UpdatedId = result.UpdatedId
        };
    }
}
