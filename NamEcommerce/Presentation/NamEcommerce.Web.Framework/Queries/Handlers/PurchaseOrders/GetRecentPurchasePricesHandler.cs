using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.PurchaseOrders;

public sealed class GetRecentPurchasePricesHandler : IRequestHandler<GetRecentPurchasePricesQuery, IList<RecentPurchasePriceModel>>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public GetRecentPurchasePricesHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<IList<RecentPurchasePriceModel>> Handle(GetRecentPurchasePricesQuery request, CancellationToken cancellationToken)
    {
        var appDtos = await _purchaseOrderAppService.GetRecentPurchasePricesAsync(request.ProductId).ConfigureAwait(false);

        return appDtos
            .Select(d => new RecentPurchasePriceModel(
                VendorId: d.VendorId,
                VendorName: d.VendorName,
                UnitCost: d.UnitCost,
                PurchaseOrderCode: d.PurchaseOrderCode,
                PurchaseDate: d.PurchaseDate))
            .ToList();
    }
}
