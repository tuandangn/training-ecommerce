namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

/// <summary>
/// Thrown khi cố xóa một GoodsReceipt đã có StockMovementLog tham chiếu (đã cộng tồn).
/// Quyết định thiết kế: cấm xóa thay vì reverse stock — tránh tồn âm khi đã bán.
/// </summary>
[Serializable]
public sealed class GoodsReceiptHasStockMovementsException(Guid id)
    : NamEcommerceDomainException("Error.GoodsReceipt.CannotDeleteHasStockMovements", id);
