using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

namespace NamEcommerce.Domain.Services.Extensions;

public static class GoodsReceiptExtensions
{
    public static GoodsReceiptDto ToDto(this GoodsReceipt goodsReceipt)
    {
        var dto = new GoodsReceiptDto(goodsReceipt.Id)
        {
            CreatedOnUtc = goodsReceipt.CreatedOnUtc,
            IsPendingCosting = goodsReceipt.IsPendingCosting(),
            Items = goodsReceipt.Items.Select(item => new GoodsReceiptItemDto(item.Id) {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                WarehouseId = item.WarehouseId,
                WarehouseName = item.WarehouseName,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost
            }),
            Note = goodsReceipt.Note,
            PictureIds = goodsReceipt.PictureIds,
            TruckDriverName = goodsReceipt.TruckDriverName,
            TruckNumberSerial = goodsReceipt.TruckNumberSerial,
            VendorId = goodsReceipt.VendorId,
            VendorName = goodsReceipt.VendorName,
            VendorPhone = goodsReceipt.VendorPhone,
            VendorAddress = goodsReceipt.VendorAddress
        };

        return dto;
    }
}
