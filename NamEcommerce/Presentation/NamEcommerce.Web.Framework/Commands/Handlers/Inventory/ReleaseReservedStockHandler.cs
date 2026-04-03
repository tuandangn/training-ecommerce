using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class ReleaseReservedStockHandler : IRequestHandler<ReleaseReservedStockCommand, ReleaseReservedStockResultModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public ReleaseReservedStockHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<ReleaseReservedStockResultModel> Handle(ReleaseReservedStockCommand request, CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.ReleaseReservedStockAsync(new ReleaseStockAppDto
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            ReferenceId = request.ReferenceId,
            UserId = request.UserId,
            Note = request.Note
        });

        return new ReleaseReservedStockResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
