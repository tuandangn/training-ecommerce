using NamEcommerce.Domain.Shared.Dtos.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IInventoryStockManager
{
    Task<StockMovementLogDto?> AdjustStockAsync(Guid productId, Guid warehouseId, decimal newQuantity, string? note, Guid modifiedByUserId);
    Task<StockMovementLogDto?> ReceiveStockAsync(Guid productId, Guid warehouseId, decimal receivedQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId);

    /// <summary>
    /// Cập nhật giá vốn bình quân (AverageCost) cho stock của (productId, warehouseId).
    /// Được gọi bởi GoodsReceiptUpdatedHandler sau khi tính lại Full Recalculation
    /// dựa trên toàn bộ GoodsReceiptItems đã có UnitCost cùng (ProductId, WarehouseId).
    /// </summary>
    /// <exception cref="InvalidStockOperationException">newAverageCost &lt; 0</exception>
    /// <exception cref="StockNotFoundException">không tìm thấy InventoryStock cho cặp (productId, warehouseId)</exception>
    Task UpdateAverageCostAsync(Guid productId, Guid warehouseId, decimal newAverageCost);
    
    Task<bool> ReserveStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null, int? reservationDaysValid = null);
    Task<bool> ReleaseReservedStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null);
    Task<StockMovementLogDto?> DispatchStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null);

    Task<(int Total, List<InventoryStockDto> Items)> GetInventoryStocksAsync(string? keywords, Guid? warehouseId, int pageIndex, int pageSize);
    //*TODO* remove warehouse id
    Task<IEnumerable<InventoryStockDto>> GetInventoryStocksForProductAsync(Guid productId, Guid? warehouseId);
    Task<(int Total, List<StockMovementLogDto> Items)> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize);
    
    /// <summary>
    /// Release all expired reservations across all stocks.
    /// Called periodically by background jobs or triggered on-demand.
    /// Default expiration: 7 days if reservationDaysValid not set.
    /// </summary>
    Task<int> ReleaseExpiredReservationsAsync();
}
