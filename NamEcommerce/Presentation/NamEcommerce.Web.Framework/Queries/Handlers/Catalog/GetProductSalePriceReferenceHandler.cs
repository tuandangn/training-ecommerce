using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductSalePriceReferenceHandler : IRequestHandler<GetProductSalePriceReferenceQuery, ProductSalePriceReferenceModel>
{
    private readonly IProductAppService _productAppService;
    private readonly IOrderAppService _orderAppService;

    public GetProductSalePriceReferenceHandler(IProductAppService productAppService, IOrderAppService orderAppService)
    {
        _productAppService = productAppService;
        _orderAppService = orderAppService;
    }

    public async Task<ProductSalePriceReferenceModel> Handle(GetProductSalePriceReferenceQuery request, CancellationToken cancellationToken)
    {
        var product = request.ProductId == Guid.Empty
            ? null
            : await _productAppService.GetProductByIdAsync(request.ProductId).ConfigureAwait(false);
        var defaultPrice = product?.UnitPrice ?? 0m;

        var customerId = request.CustomerId.GetValueOrDefault();
        IList<RecentSalePriceAppDto> recentPrices = [];
        if (customerId != Guid.Empty && request.ProductId != Guid.Empty)
            recentPrices = await _orderAppService.GetRecentSalePricesAsync(request.ProductId, customerId, request.Take).ConfigureAwait(false);
        var latestPrice = recentPrices.FirstOrDefault();
        var suggestedPrice = latestPrice?.UnitPrice ?? defaultPrice;

        return new ProductSalePriceReferenceModel
        {
            ProductId = request.ProductId,
            CustomerId = request.CustomerId,
            ProductDefaultUnitPrice = defaultPrice,
            SuggestedPrice = suggestedPrice,
            Source = latestPrice is not null ? "CustomerLastSale" : "ProductDefault",
            SourceText = latestPrice is not null ? "Đơn bán gần nhất của khách hàng" : "Giá bán mặc định của sản phẩm",
            RequiresManualInput = suggestedPrice <= 0,
            Items = recentPrices.Select(price => new ProductSalePriceReferenceModel.SalePriceReferenceItemModel
            {
                CustomerId = price.CustomerId,
                CustomerName = price.CustomerName,
                UnitPrice = price.UnitPrice,
                OrderCode = price.OrderCode,
                OrderDate = DateTimeHelper.ToLocalTime(price.OrderDateUtc),
                OrderDateText = DateTimeHelper.ToLocalTime(price.OrderDateUtc).ToString("dd/MM/yyyy")
            }).ToList()
        };
    }
}
