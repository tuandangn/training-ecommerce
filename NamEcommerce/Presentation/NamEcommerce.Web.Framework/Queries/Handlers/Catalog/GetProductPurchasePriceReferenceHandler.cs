using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductPurchasePriceReferenceHandler : IRequestHandler<GetProductPurchasePriceReferenceQuery, ProductPurchasePriceReferenceModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public GetProductPurchasePriceReferenceHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<ProductPurchasePriceReferenceModel> Handle(GetProductPurchasePriceReferenceQuery request, CancellationToken cancellationToken)
    {
        IList<RecentPurchasePriceAppDto> recentPrices = [];
        if (request.ProductId != Guid.Empty)
            recentPrices = await _purchaseOrderAppService.GetRecentPurchasePricesAsync(request.ProductId).ConfigureAwait(false);

        var vendorId = request.VendorId.GetValueOrDefault();
        var matchedPrice = vendorId == Guid.Empty
            ? null
            : recentPrices.FirstOrDefault(price => price.VendorId == vendorId);
        var suggestedCost = matchedPrice?.UnitCost ?? 0m;

        return new ProductPurchasePriceReferenceModel
        {
            ProductId = request.ProductId,
            VendorId = request.VendorId,
            SuggestedCost = suggestedCost,
            Source = matchedPrice is not null ? "VendorLastPurchase" : "NoVendorHistory",
            SourceText = matchedPrice is not null ? "Đơn nhập gần nhất của nhà cung cấp" : "Chưa có lịch sử nhập từ nhà cung cấp",
            RequiresManualInput = matchedPrice is null || suggestedCost <= 0,
            Items = recentPrices.Select(price => new ProductPurchasePriceReferenceModel.PurchasePriceReferenceItemModel
            {
                VendorId = price.VendorId,
                VendorName = price.VendorName,
                UnitCost = price.UnitCost,
                PurchaseOrderCode = price.PurchaseOrderCode,
                PurchaseDate = price.PurchaseDate,
                PurchaseDateText = price.PurchaseDate.ToString("dd/MM/yyyy")
            }).ToList()
        };
    }
}
