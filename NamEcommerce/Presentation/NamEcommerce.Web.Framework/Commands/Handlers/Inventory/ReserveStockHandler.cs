using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class ReserveStockHandler : IRequestHandler<ReserveStockCommand, ReserveStockResultModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public ReserveStockHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<ReserveStockResultModel> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.ReserveStockAsync(new ReserveStockAppDto
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            ReferenceId = request.ReferenceId,
            UserId = request.UserId,
            Note = request.Note
        });

        return new ReserveStockResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
