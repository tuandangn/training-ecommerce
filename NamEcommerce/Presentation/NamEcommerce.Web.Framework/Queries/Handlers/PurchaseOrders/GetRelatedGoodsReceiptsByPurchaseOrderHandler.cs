using MediatR;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.PurchaseOrders;

public sealed class GetRelatedGoodsReceiptsByPurchaseOrderHandler
    : IRequestHandler<GetRelatedGoodsReceiptsByPurchaseOrderQuery, IList<RelatedGoodsReceiptModel>>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public GetRelatedGoodsReceiptsByPurchaseOrderHandler(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    public async Task<IList<RelatedGoodsReceiptModel>> Handle(GetRelatedGoodsReceiptsByPurchaseOrderQuery request, CancellationToken cancellationToken)
    {
        var receipts = await _goodsReceiptAppService
            .GetGoodsReceiptsByPurchaseOrderIdAsync(request.PurchaseOrderId)
            .ConfigureAwait(false);

        return receipts.Select(receipt =>
        {
            var totalValue = receipt.IsPendingCosting
                ? (decimal?)null
                : receipt.Items.Sum(it => it.Quantity * (it.UnitCost ?? 0));

            var warehouseNames = receipt.Items
                .Select(it => it.WarehouseName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .Cast<string>()
                .ToList();

            return new RelatedGoodsReceiptModel(receipt.Id)
            {
                Code = receipt.Code,
                ReceivedOn = receipt.ReceivedOnUtc.ToLocalTime(),
                ItemCount = receipt.Items.Count,
                TotalQuantity = receipt.Items.Sum(it => it.Quantity),
                IsPendingCosting = receipt.IsPendingCosting,
                TotalValue = totalValue,
                WarehouseNames = warehouseNames,
                Items = receipt.Items.Select(it => new RelatedGoodsReceiptItemModel
                {
                    ProductName = it.ProductName ?? string.Empty,
                    Quantity = it.Quantity,
                    WarehouseName = it.WarehouseName,
                    UnitCost = it.UnitCost
                }).ToList()
            };
        }).ToList();
    }
}
