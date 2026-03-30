using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class AdjustStockHandler : IRequestHandler<AdjustStockCommand, AdjustStockResultModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public AdjustStockHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<AdjustStockResultModel> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.AdjustStockAsync(new AdjustStockAppDto
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            NewQuantity = request.NewQuantity,
            Note = request.Note,
            ModifiedByUserId = request.ModifiedByUserId
        });

        return new AdjustStockResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
