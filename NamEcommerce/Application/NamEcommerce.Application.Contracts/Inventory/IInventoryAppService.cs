using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Inventory;

namespace NamEcommerce.Application.Contracts.Inventory;

public interface IInventoryAppService
{
    Task<IPagedDataAppDto<InventoryStockAppDto>> GetInventoryStocksAsync(string? keywords, Guid? warehouseId, int pageIndex, int pageSize);
    Task<IPagedDataAppDto<StockMovementLogAppDto>> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize);
    Task<ResultAppDto> AdjustStockAsync(AdjustStockAppDto dto);
    Task<ResultAppDto> ReserveStockAsync(ReserveStockAppDto dto);
    Task<ResultAppDto> ReleaseReservedStockAsync(ReleaseStockAppDto dto);
    Task<ResultAppDto> DispatchStockAsync(DispatchStockAppDto dto);
    Task<ResultAppDto> ReceiveStockAsync(ReceiveStockAppDto dto);
}
