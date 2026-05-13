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

        var quantityAvailable = request.WarehouseId.HasValue
            ? stockItems.Sum(item => item.QuantityAvailable)
            : await _inventoryAppService.GetGlobalAvailableForProductAsync(request.ProductId).ConfigureAwait(false);

        return new ProductStockInfoModel
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            QuantityOnHand = stockItems.Sum(item => item.QuantityOnHand),
            QuantityReserved = stockItems.Sum(item => item.QuantityReserved),
            QuantityAvailable = quantityAvailable,
            AvailableWarehouseIds = stockItems.Where(item => item.WarehouseId.HasValue && item.QuantityAvailable > 0)
                .Select(item => item.WarehouseId)
                .OfType<Guid>().Distinct().ToList()
        };
    }
}
