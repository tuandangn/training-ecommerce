using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class DispatchStockHandler : IRequestHandler<DispatchStockCommand, DispatchStockResultModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public DispatchStockHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<DispatchStockResultModel> Handle(DispatchStockCommand request, CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.DispatchStockAsync(new DispatchStockAppDto
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            ReferenceId = request.ReferenceId,
            UserId = request.UserId,
            Note = request.Note
        });

        return new DispatchStockResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
