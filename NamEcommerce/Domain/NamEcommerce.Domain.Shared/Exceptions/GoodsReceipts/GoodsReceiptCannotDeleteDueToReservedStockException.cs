namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

/// <summary>
/// Không xóa được phiếu nhập kho vì lượng hàng đã được Reserve cho phiếu xuất khác
/// (DeliveryNote ở trạng thái Confirmed/Delivering) — phải hủy phiếu xuất hoặc chờ giao trả về trước.
/// </summary>
public sealed class GoodsReceiptCannotDeleteDueToReservedStockException : NamEcommerceDomainException
{
    public GoodsReceiptCannotDeleteDueToReservedStockException(Guid goodsReceiptId, Guid productId, Guid warehouseId, decimal requiredQuantity, decimal availableQuantity)
        : base("Error.GoodsReceiptCannotDeleteDueToReservedStock", goodsReceiptId, productId, warehouseId, requiredQuantity, availableQuantity)
    {
        GoodsReceiptId = goodsReceiptId;
        ProductId = productId;
        WarehouseId = warehouseId;
        RequiredQuantity = requiredQuantity;
        AvailableQuantity = availableQuantity;
    }

    public Guid GoodsReceiptId { get; }
    public Guid ProductId { get; }
    public Guid WarehouseId { get; }
    public decimal RequiredQuantity { get; }
    public decimal AvailableQuantity { get; }
}
