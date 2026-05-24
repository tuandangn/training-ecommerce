using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Inventory;

namespace NamEcommerce.Application.Contracts.Inventory;

public interface IInventoryAppService
{
    Task<IPagedDataAppDto<InventoryStockAppDto>> GetInventoryStocksAsync(int pageIndex, int pageSize, Guid? warehouseId = null, Guid? productId = null, string keywords = null);
    Task<IPagedDataAppDto<StockMovementLogAppDto>> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize);
    Task<IPagedDataAppDto<ProductReservationLedgerAppDto>> GetProductReservationLedgerAsync(Guid productId, int pageIndex, int pageSize);
    Task<decimal> GetGlobalAvailableForProductAsync(Guid productId);
    Task<SetStockLevelsResultAppDto> SetStockLevelsAsync(SetStockLevelsAppDto dto);
}
