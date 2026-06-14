using NamEcommerce.Domain.Shared.Dtos.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IInventoryStockManager
{
    Task InitializeStockAsync(Guid productId, Guid warehouseId, Guid? unitMeasurementId = null);

    Task<StockMovementLogDto?> ReceiveStockAsync(Guid productId, Guid warehouseId, decimal receivedQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId, bool enforceCapacity = true);
    Task<StockMovementLogDto?> ReceiveStockUpToAsync(Guid productId, Guid warehouseId, decimal targetQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId, bool enforceCapacity = true);

    Task<StockMovementLogDto?> RevertReceiveAsync(Guid productId, Guid warehouseId, decimal quantity, Guid goodsReceiptId, Guid modifiedByUserId);
    Task<StockMovementLogDto?> RevertReceiveUpToAsync(Guid productId, Guid warehouseId, decimal targetQuantity, Guid goodsReceiptId, Guid modifiedByUserId);

    Task<bool> ReserveStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null, int? reservationDaysValid = null);
    Task<(StockMovementLogDto? OutLog, StockMovementLogDto? InLog)> TransferStockUpToAsync(Guid productId, Guid fromWarehouseId, Guid toWarehouseId, decimal targetQuantity, decimal unitCost, Guid? referenceId, Guid userId, string? note = null);
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
    Task<decimal> ComputeAvailableQuantityForOrderAsync(Guid productId, Guid orderId);
    Task<(int Total, List<StockMovementLogDto> Items)> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize);

    Task ApplyAdjustmentAsync(Guid productId, Guid warehouseId, decimal delta, Guid adjustmentNoteId, Guid? userId);

    [Obsolete("Expiry-based reservation release is replaced by per-reference ledger (StockReservationEntry). This method is a no-op.")]
    Task<int> ReleaseExpiredReservationsAsync();

    Task SetStockLevelsAsync(SetStockLevelsDto dto);
}
