using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.PurchaseOrders;

public sealed class GetRelatedVendorReturnsByPurchaseOrderHandler
    : IRequestHandler<GetRelatedVendorReturnsByPurchaseOrderQuery, IList<RelatedVendorReturnModel>>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public GetRelatedVendorReturnsByPurchaseOrderHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public async Task<IList<RelatedVendorReturnModel>> Handle(GetRelatedVendorReturnsByPurchaseOrderQuery request, CancellationToken cancellationToken)
    {
        var (_, items) = await _vendorReturnAppService.GetListAsync(
            pageIndex: 0,
            pageSize: int.MaxValue,
            vendorId: null,
            purchaseOrderId: request.PurchaseOrderId,
            goodsReceiptId: null,
            status: null).ConfigureAwait(false);

        return items
            .OrderByDescending(vr => vr.ReturnDate)
            .Select(vr => new RelatedVendorReturnModel(vr.Id)
            {
                Code = vr.Code,
                ReturnDate = vr.ReturnDate.ToLocalTime(),
                Status = vr.Status,
                WarehouseName = vr.WarehouseName,
                ItemCount = vr.Items.Count(),
                TotalQuantity = vr.Items.Sum(it => it.AcceptedQuantity),
                TotalAmount = vr.NetRecoveryAmount,
                AdditionalCost = vr.AdditionalCost,
                Items = vr.Items.Select(it => new RelatedVendorReturnItemModel
                {
                    ProductName = it.ProductName,
                    RequestedQuantity = it.RequestedQuantity,
                    AcceptedQuantity = it.AcceptedQuantity,
                    ReturnUnitCost = it.ReturnUnitCost
                }).ToList()
            }).ToList();
    }
}
