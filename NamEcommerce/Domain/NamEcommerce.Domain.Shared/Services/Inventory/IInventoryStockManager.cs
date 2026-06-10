using NamEcommerce.Domain.Shared.Dtos.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IInventoryStockManager
{
    Task InitializeStockAsync(Guid productId, Guid warehouseId, Guid? unitMeasurementId = null);

    Task<StockMovementLogDto?> ReceiveStockAsync(Guid productId, Guid warehouseId, decimal receivedQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId);
    Task<StockMovementLogDto?> ReceiveStockUpToAsync(Guid productId, Guid warehouseId, decimal targetQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId);

    /// <summary>
    /// Hoàn tác nhập hàng — trừ lại <paramref name="quantity"/> đơn vị đã nhập trước đó từ GoodsReceipt bị xóa.
    /// Khác với AdjustStockAsync (set tuyệt đối), method này trừ đúng số lượng đã nhập để bảo toàn audit trail.
    /// Ghi log movement type = Revert (âm).
    /// </summary>
    /// <exception cref="InvalidStockOperationException">quantity &lt;= 0 hoặc stock không tồn tại</exception>
    Task<StockMovementLogDto?> RevertReceiveAsync(Guid productId, Guid warehouseId, decimal quantity, Guid goodsReceiptId, Guid modifiedByUserId);
    Task<StockMovementLogDto?> RevertReceiveUpToAsync(Guid productId, Guid warehouseId, decimal targetQuantity, Guid goodsReceiptId, Guid modifiedByUserId);

    Task<bool> ReserveStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null, int? reservationDaysValid = null);
    Task<bool> ReleaseReservedStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null);
    Task<StockMovementLogDto?> DispatchStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null, bool releaseReservedStock = false, int referenceType = 2);
    Task<StockMovementLogDto?> DispatchStockUpToAsync(Guid productId, Guid warehouseId, decimal targetQuantity, Guid? referenceId, Guid userId, string? note = null, bool releaseReservedStock = false, int referenceType = 2);
    Task<(StockMovementLogDto? OutLog, StockMovementLogDto? InLog)> TransferStockAsync(
        Guid productId,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        decimal quantity,
        decimal unitCost,
        Guid? referenceId,
        Guid userId,
        string? note = null);

    Task<(int Total, List<InventoryStockDto> Items)> GetInventoryStocksAsync(int pageIndex, int pageSize, Guid? warehouseId = null, Guid? productId = null, string? keywords = null);
    Task<(int Total, List<InventoryStockDto> Items)> GetInventoryStocksAsync(int pageIndex, int pageSize, Guid?[]? warehouseIds = null, Guid?[]? productIds = null, string? keywords = null);
    Task<InventoryStockDto?> GetInventoryStockForProductAsync(Guid productId, Guid warehouseId);
    Task<IEnumerable<InventoryStockDto>> GetInventoryStocksForProductAsync(Guid productId);
    Task<decimal> GetGlobalAvailableQuantityForProductAsync(Guid productId, Guid? excludeOrderId = null);
    Task<(int Total, List<StockMovementLogDto> Items)> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize);
    
    /// <summary>
    /// Legacy compatibility: lấy AverageCost snapshot hiện tại của (productId, warehouseId).
    /// Trả về 0 nếu chưa có stock hoặc chưa nhập hàng.
    /// Không dùng làm nguồn authoritative cho COGS.
    /// </summary>
    Task<decimal> GetAverageCostAsync(Guid productId, Guid warehouseId);

    /// <summary>
    /// Áp dụng điều chỉnh tồn kho dựa trên delta từ StockAdjustmentNote.
    /// Delta dương → nhập thêm (MovementType=Adjustment, Inbound).
    /// Delta âm → xuất bớt (MovementType=Adjustment, Outbound).
    /// Delta = 0 → bỏ qua.
    /// Ghi StockMovementLog với ReferenceType=Adjustment và ReferenceId=adjustmentNoteId.
    /// </summary>
    Task ApplyAdjustmentAsync(Guid productId, Guid warehouseId, decimal delta, Guid adjustmentNoteId, Guid? userId);

    /// <summary>
    /// Release all expired reservations across all stocks.
    /// Called periodically by background jobs or triggered on-demand.
    /// Default expiration: 7 days if reservationDaysValid not set.
    /// </summary>
    Task<int> ReleaseExpiredReservationsAsync();

    /// <summary>
    /// Cập nhật ngưỡng cảnh báo tồn kho (ReorderLevel) và ngưỡng tối đa (MaxStockLevel) cho một stock record.
    /// </summary>
    /// <exception cref="StockNotFoundException">không tìm thấy InventoryStock theo Id</exception>
    /// <exception cref="InvalidStockOperationException">dto không hợp lệ (ReorderLevel/MaxStockLevel âm hoặc MaxStockLevel &lt; ReorderLevel)</exception>
    Task SetStockLevelsAsync(SetStockLevelsDto dto);
}
