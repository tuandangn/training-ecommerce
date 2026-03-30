using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Framework.Common;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetStockMovementLogsHandler : IRequestHandler<GetStockMovementLogsQuery, StockMovementLogListModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public GetStockMovementLogsHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<StockMovementLogListModel> Handle(GetStockMovementLogsQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _inventoryAppService.GetStockMovementLogsAsync(request.ProductId, request.WarehouseId, request.PageIndex, request.PageSize);

        var model = new StockMovementLogListModel
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            Data = pagedData.MapToModel(item => new StockMovementLogListModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                MovementType = item.MovementType,
                Quantity = item.Quantity,
                QuantityBefore = item.QuantityBefore,
                QuantityAfter = item.QuantityAfter,
                CreatedOnUtc = item.CreatedOnUtc,
                Note = item.Note
            })
        };

        return model;
    }
}
