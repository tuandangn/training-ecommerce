using MediatR;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Queries.Models.GoodsReceipts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.GoodsReceipts;

public sealed class GetGoodsReceiptHandler : IRequestHandler<GetGoodsReceiptQuery, GoodsReceiptModel?>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public GetGoodsReceiptHandler(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    public async Task<GoodsReceiptModel?> Handle(GetGoodsReceiptQuery request, CancellationToken cancellationToken)
    {
        var goodsReceipt = await _goodsReceiptAppService.GetGoodsReceiptByIdAsync(request.Id).ConfigureAwait(false);
        if (goodsReceipt is null)
            return null;

        var model = new GoodsReceiptModel
        {
            Id = goodsReceipt.Id,
            CreatedOn = goodsReceipt.CreatedOnUtc.ToLocalTime(),
            TruckDriverName = goodsReceipt.TruckDriverName,
            TruckNumberSerial = goodsReceipt.TruckNumberSerial,
            PictureIds = goodsReceipt.PictureIds,
            Note = goodsReceipt.Note,
            IsPendingCosting = goodsReceipt.IsPendingCosting
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
