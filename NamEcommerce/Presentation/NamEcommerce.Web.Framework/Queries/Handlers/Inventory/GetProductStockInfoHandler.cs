using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetProductStockInfoHandler : IRequestHandler<GetProductStockInfoQuery, ProductStockInfoModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public GetProductStockInfoHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<ProductStockInfoModel> Handle(GetProductStockInfoQuery request, CancellationToken cancellationToken)
    {
        var stockItems = await _inventoryAppService.GetInventoryStocksAsync(0, int.MaxValue, request.WarehouseId, request.ProductId).ConfigureAwait(false);

        var availableQty = 0m;
        var reservedQty = 0m;
        if (request.WarehouseId.HasValue)
        {
            availableQty = stockItems.Sum(item => item.QuantityAvailable);
            reservedQty = stockItems.Sum(item => item.QuantityReserved);
        }
        else
        {
            (availableQty, reservedQty) = await _inventoryAppService.GetGlobalAvailableQuantityForProductAsync(request.ProductId).ConfigureAwait(false);
        }

        return new ProductStockInfoModel
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            QuantityOnHand = stockItems.Sum(item => item.QuantityOnHand),
            QuantityReserved = reservedQty,
            QuantityAvailable = availableQty,
            QuantityDecimalPlaces = stockItems.FirstOrDefault()?.QuantityDecimalPlaces ?? 0,
            AvailableWarehouseIds = stockItems.Where(item => item.QuantityAvailable > 0)
                .Select(item => item.WarehouseId)
                .OfType<Guid>().Distinct().ToList(),
            Warehouses = stockItems
                .Where(item => item.WarehouseId != Guid.Empty)
                .Select(item => new ProductStockWarehouseInfoModel
                {
                    WarehouseId = item.WarehouseId,
                    WarehouseName = item.WarehouseName,
                    QuantityOnHand = item.QuantityOnHand,
                    QuantityReserved = item.QuantityReserved,
                    QuantityAvailable = item.QuantityAvailable,
                    QuantityDecimalPlaces = item.QuantityDecimalPlaces
                })
                .ToList()
        };
    }
}
