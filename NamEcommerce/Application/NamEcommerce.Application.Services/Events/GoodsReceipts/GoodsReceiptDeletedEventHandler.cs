using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

public sealed class GoodsReceiptDeletedEventHandler(
    IRepository<Picture> pictureRepository,
    IEntityDataReader<Picture> pictureDataReader,
    IInventoryStockManager inventoryStockManager) : INotificationHandler<EntityDeletedNotification<GoodsReceipt>>
{
    public async Task Handle(EntityDeletedNotification<GoodsReceipt> notification, CancellationToken cancellationToken)
    {
        var goodsReceipt = notification.Entity;
        if (goodsReceipt is null) return;

        foreach (var item in goodsReceipt.Items)
        {
            if (!item.WarehouseId.HasValue) continue;
            if (item.Quantity <= 0) continue;

            var stock = (await inventoryStockManager.GetInventoryStocksForProductAsync(item.ProductId, item.WarehouseId.Value).ConfigureAwait(false)).Single();
            await inventoryStockManager.AdjustStockAsync(
                productId: item.ProductId,
                warehouseId: item.WarehouseId.Value,
                newQuantity: stock.QuantityAvailable - item.Quantity,
                note: $"Thay đổi do phiếu {goodsReceipt.Id} bị xóa",
                modifiedByUserId: goodsReceipt.CreatedByUserId ?? Guid.Empty
            ).ConfigureAwait(false);
        }

        if (!goodsReceipt.PictureIds.Any())
            return;

        foreach (var pictureId in goodsReceipt.PictureIds)
        {
            var picture = await pictureDataReader.GetByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await pictureRepository.DeleteAsync(picture).ConfigureAwait(false);
        }
    }
}
