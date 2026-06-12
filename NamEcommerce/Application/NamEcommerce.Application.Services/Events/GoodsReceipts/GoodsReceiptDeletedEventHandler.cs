using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Events.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Media;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

/// <summary>
/// Handler cho <see cref="GoodsReceiptDeleted"/>. Sau khi phiếu nhập bị xoá:
/// <list type="number">
///   <item><description><b>Hoàn nguyên tồn kho:</b> với mỗi item có <c>WarehouseId</c>, gọi
///     <see cref="IInventoryStockManager.RevertReceiveAsync"/> để trừ đúng số lượng đã nhập.</description></item>
///   <item><description><b>Xoá ảnh đính kèm:</b> dọn các <see cref="Picture"/> theo
///     <see cref="GoodsReceiptDeleted.PictureIds"/> (event đã capture danh sách ảnh trước khi xoá).</description></item>
/// </list>
///
/// <para>Lưu ý: phiếu nhập đã có stock movements bị block xóa từ phía Manager
/// (<c>InsufficientStockException</c> trong <c>DeleteGoodsReceiptAsync</c>). Handler này chỉ
/// chạy khi phiếu thực sự xoá thành công.</para>
/// </summary>
public sealed class GoodsReceiptDeletedEventHandler : INotificationHandler<GoodsReceiptDeleted>
{
    private readonly IPictureManager _pictureManager;
    private readonly IEntityDataReader<GoodsReceipt> _goodsReceiptDataReader;
    private readonly IInventoryStockManager _inventoryStockManager;
    private readonly IInventoryCostingManager _inventoryCostingManager;

    public GoodsReceiptDeletedEventHandler(
        IPictureManager pictureManager,
        IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
        IInventoryStockManager inventoryStockManager,
        IInventoryCostingManager inventoryCostingManager)
    {
        _pictureManager = pictureManager;
        _goodsReceiptDataReader = goodsReceiptDataReader;
        _inventoryStockManager = inventoryStockManager;
        _inventoryCostingManager = inventoryCostingManager;
    }

    public async Task Handle(GoodsReceiptDeleted notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        // Re-fetch entity (soft delete vẫn cho GetByIdAsync trả entity với các Items được hydrate
        // — cần Items để hoàn nguyên tồn kho).
        var goodsReceipt = await _goodsReceiptDataReader.GetByIdAsync(notification.GoodsReceiptId, default).ConfigureAwait(false);
        if (goodsReceipt is not null)
        {
            foreach (var item in goodsReceipt.Items)
            {
                if (!item.WarehouseId.HasValue) continue;
                if (item.Quantity <= 0) continue;

                await _inventoryStockManager.RevertReceiveUpToAsync(
                    productId: item.ProductId,
                    warehouseId: item.WarehouseId.Value,
                    targetQuantity: goodsReceipt.Items
                        .Where(i => i.ProductId == item.ProductId && i.WarehouseId == item.WarehouseId)
                        .Sum(i => i.Quantity),
                    goodsReceiptId: goodsReceipt.Id,
                    modifiedByUserId: goodsReceipt.CreatedByUserId ?? Guid.Empty
                ).ConfigureAwait(false);

                await _inventoryCostingManager.RegisterReceiptReversalAsync(new RegisterInventoryReceiptReversalCostDto
                {
                    ProductId = item.ProductId,
                    WarehouseId = item.WarehouseId.Value,
                    Quantity = item.Quantity,
                    GoodsReceiptId = goodsReceipt.Id,
                    GoodsReceiptItemId = item.Id,
                    OccurredAtUtc = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
        }

        // Dọn ảnh — danh sách PictureIds đã capture trong event (trước khi xoá).
        if (notification.PictureIds is null || notification.PictureIds.Count == 0)
            return;

        foreach (var pictureId in notification.PictureIds)
        {
            var picture = await _pictureManager.GetPictureByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await _pictureManager.DeletePictureAsync(pictureId).ConfigureAwait(false);
        }
    }
}
