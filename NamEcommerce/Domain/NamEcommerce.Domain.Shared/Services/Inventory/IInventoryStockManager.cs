using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NamEcommerce.Domain.Shared.Dtos.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IInventoryStockManager
{
    Task<StockMovementLogDto?> AdjustStockAsync(Guid productId, Guid warehouseId, decimal newQuantity, string? note, Guid modifiedByUserId);
    Task<(int Total, List<InventoryStockDto> Items)> GetInventoryStocksAsync(string? keywords, Guid? warehouseId, int pageIndex, int pageSize);
    Task<(int Total, List<StockMovementLogDto> Items)> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize);
}
