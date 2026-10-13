using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductListForOrderHandler(IProductAppService productAppService, IMediator mediator)
    : IRequestHandler<GetProductListForOrderQuery, ProductListForOrderModel>
{
    public async Task<ProductListForOrderModel> Handle(GetProductListForOrderQuery request, CancellationToken cancellationToken)
    {
        var products = await productAppService.GetProductsAsync(0, int.MaxValue, request.Keywords, request.CategoryId, request.VendorId).ConfigureAwait(false);

        var vendorOptions = await mediator.Send(new GetVendorOptionListQuery(), cancellationToken).ConfigureAwait(false);
        var categoryOptions = await mediator.Send(new GetCategoryOptionListQuery
        {
            BreadcrumbOpts = new BreadcrumbOptions
            {
                Disabled = true
            }
        }, cancellationToken).ConfigureAwait(false);
        var warehouseOptions = await mediator.Send(new GetWarehouseOptionListQuery(), cancellationToken).ConfigureAwait(false);

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
                PictureUrl = PictureHelper.GetPictureUrl(productInfo.Pictures.FirstOrDefault()),
                UnitMeasurement = productInfo.UnitMeasurement,
                QuantityDecimalPlaces = productInfo.QuantityDecimalPlaces ?? 2
            };

            var stockInfo = await mediator.Send(new GetProductStockInfoQuery(productInfo.Id, request.WarehouseId), cancellationToken).ConfigureAwait(false);
            productModel.QuantityOnHand = stockInfo.QuantityOnHand;
            productModel.QuantityReserved = stockInfo.QuantityReserved;
            productModel.QuantityAvailable = stockInfo.QuantityAvailable;

            productModel.AvailableWarehouses = warehouseOptions.Where(option => stockInfo.AvailableWarehouseIds.Contains(option.Id)).ToList();
            productModel.AvailableWarehouseStocks = stockInfo.Warehouses
                .Where(stock => stock.QuantityAvailable > 0)
                .Select(stock => new ProductListForOrderModel.ProductWarehouseStockModel
                {
                    Id = stock.WarehouseId,
                    Name = warehouseOptions.FirstOrDefault(option => option.Id == stock.WarehouseId)?.Name ?? stock.WarehouseName,
                    QuantityOnHand = stock.QuantityOnHand,
                    QuantityReserved = stock.QuantityReserved,
                    QuantityAvailable = stock.QuantityAvailable
                })
                .ToList();

            if (productInfo.Vendors != null)
                productModel.AvailableVendors = vendorOptions.Where(option => productInfo.Vendors.Any(v => v.VendorId == option.Id)).ToList();

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
