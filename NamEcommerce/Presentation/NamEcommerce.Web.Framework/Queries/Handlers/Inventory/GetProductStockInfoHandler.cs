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
        var stockItems = await _inventoryAppService.GetInventoryStocksForProductAsync(request.ProductId, request.WarehouseId);

        return new ProductStockInfoModel
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            QuantityOnHand = stockItems.Sum(item => item.QuantityOnHand),
            QuantityReserved = stockItems.Sum(item => item.QuantityReserved),
            QuantityAvailable = stockItems.Sum(item => item.QuantityAvailable)
        };
    }
}
