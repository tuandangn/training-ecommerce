using MediatR;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Queries.Models.GoodsReceipts;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.GoodsReceipts;

public sealed class GetSuggestedPurchaseOrdersForGoodsReceiptHandler
    : IRequestHandler<GetSuggestedPurchaseOrdersForGoodsReceiptQuery, IList<SuggestedPurchaseOrderModel>>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public GetSuggestedPurchaseOrdersForGoodsReceiptHandler(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    public async Task<IList<SuggestedPurchaseOrderModel>> Handle(
        GetSuggestedPurchaseOrdersForGoodsReceiptQuery request,
        CancellationToken cancellationToken)
    {
        var appDtoList = await _goodsReceiptAppService
            .GetSuggestedPurchaseOrdersAsync(request.GoodsReceiptId)
            .ConfigureAwait(false);

        return appDtoList.Select(po => new SuggestedPurchaseOrderModel
        {
            PurchaseOrderId = po.PurchaseOrderId,
            PurchaseOrderCode = po.PurchaseOrderCode,
            PlacedOn = DateTimeHelper.ToLocalTime(po.PlacedOnUtc),
            VendorId = po.VendorId,
            MatchScore = po.MatchScore,
            IsFullMatch = po.IsFullMatch,
            Items = po.Items.Select(i => new SuggestedPurchaseOrderModel.ItemModel
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                QuantityOrdered = i.QuantityOrdered,
                QuantityReceived = i.QuantityReceived,
                UnitCost = i.UnitCost
            }).ToList()
        }).ToList();
    }
}
