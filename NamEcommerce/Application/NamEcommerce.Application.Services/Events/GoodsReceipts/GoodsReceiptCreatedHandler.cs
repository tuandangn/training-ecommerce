using MediatR;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

/// <summary>
/// Sau khi tạo phiếu nhập, cộng số lượng tồn kho cho từng item có WarehouseId.
/// Mỗi lần cộng tồn sẽ ghi 1 StockMovementLog với
/// (ReferenceType = GoodsReceipt, ReferenceId = goodsReceipt.Id) — log này
/// dùng làm khóa chặn việc xóa phiếu (xem GoodsReceiptManager.DeleteGoodsReceiptAsync).
/// </summary>
public sealed class GoodsReceiptCreatedHandler : INotificationHandler<EntityCreatedNotification<GoodsReceipt>>
{
    private readonly IInventoryStockManager _inventoryStockManager;

    public GoodsReceiptCreatedHandler(IInventoryStockManager inventoryStockManager)
    {
        _inventoryStockManager = inventoryStockManager;
    }

    public async Task Handle(EntityCreatedNotification<GoodsReceipt> notification, CancellationToken cancellationToken)
    {
        var goodsReceipt = notification.Entity;
        if (goodsReceipt is null) return;

        foreach (var item in goodsReceipt.Items)
        {
            // Item không có warehouse (chế độ AllowNonWarehouse) → không cộng tồn.
            if (!item.WarehouseId.HasValue) continue;
            if (item.Quantity <= 0) continue;

            await _inventoryStockManager.ReceiveStockAsync(
                productId: item.ProductId,
                warehouseId: item.WarehouseId.Value,
                receivedQuantity: item.Quantity,
                note: $"Nhập từ phiếu {goodsReceipt.Id}",
                receivedByUserId: goodsReceipt.CreatedByUserId,
                referenceType: (int)StockReferenceType.GoodsReceipt,
                referenceId: goodsReceipt.Id
            ).ConfigureAwait(false);
        }
    }
}
