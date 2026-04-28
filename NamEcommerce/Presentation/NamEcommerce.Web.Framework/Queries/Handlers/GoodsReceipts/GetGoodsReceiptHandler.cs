using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Queries.Models.GoodsReceipts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.GoodsReceipts;

public sealed class GetGoodsReceiptHandler : IRequestHandler<GetGoodsReceiptQuery, GoodsReceiptModel?>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;
    private readonly IVendorDebtAppService _vendorDebtAppService;

    public GetGoodsReceiptHandler(
        IGoodsReceiptAppService goodsReceiptAppService,
        IVendorDebtAppService vendorDebtAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
        _vendorDebtAppService = vendorDebtAppService;
    }

    public async Task<GoodsReceiptModel?> Handle(GetGoodsReceiptQuery request, CancellationToken cancellationToken)
    {
        var goodsReceipt = await _goodsReceiptAppService.GetGoodsReceiptByIdAsync(request.Id).ConfigureAwait(false);
        if (goodsReceipt is null)
            return null;

        // Hiển thị trạng thái sinh công nợ NCC để UI render badge "Đã ghi nợ" + link sang trang VendorDebt.
        var vendorDebt = await _vendorDebtAppService.GetDebtByGoodsReceiptIdAsync(request.Id).ConfigureAwait(false);

        var model = new GoodsReceiptModel
        {
            Id = goodsReceipt.Id,
            ReceivedOn = goodsReceipt.ReceivedOnUtc.ToLocalTime(),
            TruckDriverName = goodsReceipt.TruckDriverName,
            TruckNumberSerial = goodsReceipt.TruckNumberSerial,
            PictureIds = goodsReceipt.PictureIds,
            Note = goodsReceipt.Note,
            IsPendingCosting = goodsReceipt.IsPendingCosting,
            VendorId = goodsReceipt.VendorId,
            VendorName = goodsReceipt.VendorName,
            VendorPhone = goodsReceipt.VendorPhone,
            VendorAddress = goodsReceipt.VendorAddress,
            HasVendorDebt = vendorDebt is not null,
            VendorDebtId = vendorDebt?.Id,
            VendorDebtTotalAmount = vendorDebt?.TotalAmount,
            PurchaseOrderId = goodsReceipt.PurchaseOrderId,
            PurchaseOrderCode = goodsReceipt.PurchaseOrderCode
        };

        foreach (var item in goodsReceipt.Items)
        {
            model.Items.Add(new GoodsReceiptModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                WarehouseId = item.WarehouseId,
                WarehouseName = item.WarehouseName,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                IsPendingCosting = item.IsPendingCosting
            });
        }

        return model;
    }
}
