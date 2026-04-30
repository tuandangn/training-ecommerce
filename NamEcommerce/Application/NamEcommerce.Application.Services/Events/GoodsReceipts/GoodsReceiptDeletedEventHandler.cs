using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

/// <summary>
/// Handler cho <see cref="GoodsReceiptDeleted"/>. Sau khi phiếu nhập bị xoá:
/// <list type="number">
///   <item><description><b>Hoàn nguyên tồn kho:</b> với mỗi item có <c>WarehouseId</c>, gọi
///     <see cref="IInventoryStockManager.AdjustStockAsync"/> để trừ số lượng đã cộng.</description></item>
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
    private readonly IRepository<Picture> _pictureRepository;
    private readonly IEntityDataReader<Picture> _pictureDataReader;
    private readonly IEntityDataReader<GoodsReceipt> _goodsReceiptDataReader;
    private readonly IInventoryStockManager _inventoryStockManager;

    public GoodsReceiptDeletedEventHandler(
        IRepository<Picture> pictureRepository,
        IEntityDataReader<Picture> pictureDataReader,
        IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
        IInventoryStockManager inventoryStockManager)
    {
        _pictureRepository = pictureRepository;
        _pictureDataReader = pictureDataReader;
        _goodsReceiptDataReader = goodsReceiptDataReader;
        _inventoryStockManager = inventoryStockManager;
    }

    public async Task Handle(GoodsReceiptDeleted notification, CancellationToken cancellationToken)
    {
        // Re-fetch entity (soft delete vẫn cho GetByIdAsync trả entity với các Items được hydrate
        // — cần Items để hoàn nguyên tồn kho).
        var goodsReceipt = await _goodsReceiptDataReader.GetByIdAsync(notification.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is not null)
        {
            foreach (var item in goodsReceipt.Items)
            {
                if (!item.WarehouseId.HasValue) continue;
                if (item.Quantity <= 0) continue;

                var stock = (await _inventoryStockManager.GetInventoryStocksForProductAsync(item.ProductId, item.WarehouseId.Value).ConfigureAwait(false)).Single();
                await _inventoryStockManager.AdjustStockAsync(
                    productId: item.ProductId,
                    warehouseId: item.WarehouseId.Value,
                    newQuantity: stock.QuantityAvailable - item.Quantity,
                    note: $"Thay đổi do phiếu {goodsReceipt.Id} bị xóa",
                    modifiedByUserId: goodsReceipt.CreatedByUserId ?? Guid.Empty
                ).ConfigureAwait(false);
            }
        }

        // Dọn ảnh — danh sách PictureIds đã capture trong event (trước khi xoá).
        if (notification.PictureIds is null || notification.PictureIds.Count == 0)
            return;

        foreach (var pictureId in notification.PictureIds)
        {
            var picture = await _pictureDataReader.GetByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await _pictureRepository.DeleteAsync(picture).ConfigureAwait(false);
        }
    }
}
