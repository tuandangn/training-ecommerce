using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductListForOrderHandler : IRequestHandler<GetProductListForOrderQuery, ProductListForOrderModel>
{
    private readonly IProductAppService _productAppService;
    private readonly IPictureAppService _pictureAppService;
    private readonly IMediator _mediator;

    public GetProductListForOrderHandler(IProductAppService productAppService, IMediator mediator, IPictureAppService pictureAppService)
    {
        _productAppService = productAppService;
        _mediator = mediator;
        _pictureAppService = pictureAppService;
    }

    public async Task<ProductListForOrderModel> Handle(GetProductListForOrderQuery request, CancellationToken cancellationToken)
    {
        var products = await _productAppService.GetProductsAsync(0, int.MaxValue, request.Keywords, request.CategoryId, request.VendorId).ConfigureAwait(false);

        var vendorOptions = await _mediator.Send(new GetVendorOptionListQuery(), cancellationToken).ConfigureAwait(false);
        var categoryOptions = await _mediator.Send(new GetCategoryOptionListQuery
        {
            BreadcrumbOpts = new BreadcrumbOptions
            {
                Disabled = true
            }
        }, cancellationToken).ConfigureAwait(false);
        var unitMeasurementOptions = await _mediator.Send(new GetUnitMeasurementOptionListQuery(), cancellationToken).ConfigureAwait(false);
        var warehouseOptions = await _mediator.Send(new GetWarehouseOptionListQuery(), cancellationToken).ConfigureAwait(false);

        var productListItems = new List<ProductListForOrderModel.ProductItemModel>();
        foreach (var productInfo in products)
        {
            var firstCategoryId = productInfo.Categories.FirstOrDefault()?.CategoryId;
            var categoryName = firstCategoryId.HasValue
                ? categoryOptions.Options.FirstOrDefault(c => c.Id == firstCategoryId.Value)?.Name
                : null;

            var productModel = new ProductListForOrderModel.ProductItemModel(productInfo.Id)
            {
                Name = productInfo.Name,
                UnitPrice = productInfo.UnitPrice,
                CategoryName = categoryName,
                PictureUrl = PictureHelper.GetPictureUrl(productInfo.Pictures.FirstOrDefault())
            };

            var stockInfo = await _mediator.Send(new GetProductStockInfoQuery(productInfo.Id, request.WarehouseId), cancellationToken).ConfigureAwait(false);
            productModel.QuantityOnHand = stockInfo.QuantityOnHand;
            productModel.QuantityReserved = stockInfo.QuantityReserved;
            productModel.QuantityAvailable = stockInfo.QuantityAvailable;
            productModel.AvailableWarehouses = warehouseOptions.Where(option => stockInfo.AvailableWarehouseIds.Contains(option.Id)).ToList();

            if (productInfo.Vendors != null)
                productModel.AvailableVendors = vendorOptions.Where(option => productInfo.Vendors.Any(v => v.VendorId == option.Id)).ToList();

            productModel.UnitMeasurement = unitMeasurementOptions.FirstOrDefault(option => option.Id == productInfo.UnitMeasurementId)?.Name;

            productListItems.Add(productModel);
        }

        var filteredByVendorName = request.VendorId.HasValue
            ? vendorOptions.FirstOrDefault(v => v.Id == request.VendorId.Value)?.Name
            : null;
        return new ProductListForOrderModel
        {
            Keywords = request.Keywords,
            FilteredByVendorName = filteredByVendorName,
            Data = PagedDataModel.Create(productListItems)
        };
    }
}
