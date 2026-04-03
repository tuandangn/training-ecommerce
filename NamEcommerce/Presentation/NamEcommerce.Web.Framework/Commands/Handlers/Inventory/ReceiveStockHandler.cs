using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class ReceiveStockHandler : IRequestHandler<ReceiveStockCommand, ReceiveStockResultModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public ReceiveStockHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<ReceiveStockResultModel> Handle(ReceiveStockCommand request, CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.ReceiveStockAsync(new ReceiveStockAppDto
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            UserId = request.UserId,
            Note = request.Note
        });

        return new ReceiveStockResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
