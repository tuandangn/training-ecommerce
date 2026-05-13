using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Inventory;

namespace NamEcommerce.Application.Contracts.Inventory;

public interface IInventoryAppService
{
    Task<IPagedDataAppDto<InventoryStockAppDto>> GetInventoryStocksAsync(string? keywords, Guid? warehouseId, int pageIndex, int pageSize);
    Task<IPagedDataAppDto<StockMovementLogAppDto>> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize);
    Task<IPagedDataAppDto<ProductReservationLedgerAppDto>> GetProductReservationLedgerAsync(Guid productId, int pageIndex, int pageSize);
    Task<IEnumerable<ProductInventoryStockInfoAppDto>> GetInventoryStocksForProductAsync(Guid productId, Guid? warehouseId);
    Task<decimal> GetGlobalAvailableForProductAsync(Guid productId);
}
