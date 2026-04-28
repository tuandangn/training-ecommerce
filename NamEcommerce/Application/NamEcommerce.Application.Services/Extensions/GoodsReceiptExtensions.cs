using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

namespace NamEcommerce.Application.Services.Extensions;

public static class GoodsReceiptExtensions
{
    public static GoodsReceiptAppDto ToDto(this GoodsReceiptDto goodsReceipt)
    {
        var dto = new GoodsReceiptAppDto(goodsReceipt.Id)
        {
            ReceivedOnUtc = goodsReceipt.ReceivedOnUtc,
            TruckDriverName = goodsReceipt.TruckDriverName,
            TruckNumberSerial = goodsReceipt.TruckNumberSerial,
            PictureIds = goodsReceipt.PictureIds,
            Note = goodsReceipt.Note,
            IsPendingCosting = goodsReceipt.IsPendingCosting,
            VendorId = goodsReceipt.VendorId,
            VendorName = goodsReceipt.VendorName,
            VendorPhone = goodsReceipt.VendorPhone,
            VendorAddress = goodsReceipt.VendorAddress,
            PurchaseOrderId = goodsReceipt.PurchaseOrderId,
            PurchaseOrderCode = goodsReceipt.PurchaseOrderCode
        };

        foreach (var item in goodsReceipt.Items)
        {
            dto.Items.Add(new GoodsReceiptItemAppDto(item.Id)
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                WarehouseId = item.WarehouseId,
                WarehouseName = item.WarehouseName,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                IsPendingCosting = !item.UnitCost.HasValue
            });
        }

        return dto;
    }
}
