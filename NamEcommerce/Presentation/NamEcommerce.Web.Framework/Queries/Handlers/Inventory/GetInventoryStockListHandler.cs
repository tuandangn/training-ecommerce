using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Framework.Common;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetInventoryStockListHandler : IRequestHandler<GetInventoryStockListQuery, InventoryStockListModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public GetInventoryStockListHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<InventoryStockListModel> Handle(GetInventoryStockListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _inventoryAppService.GetInventoryStocksAsync(request.Keywords, request.WarehouseId, request.PageIndex, request.PageSize);

        var model = new InventoryStockListModel
        {
            Keywords = request.Keywords,
            WarehouseId = request.WarehouseId,
            Data = pagedData.MapToModel(item => new InventoryStockListModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                WarehouseId = item.WarehouseId,
                WarehouseName = item.WarehouseName,
                QuantityOnHand = item.QuantityOnHand,
                QuantityReserved = item.QuantityReserved,
                QuantityAvailable = item.QuantityAvailable,
                UpdatedOnUtc = item.UpdatedOnUtc
            })
        };

        return model;
    }
}
