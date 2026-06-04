using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductsByIdsForOrderHandler : IRequestHandler<GetProductsByIdsForOrderQuery, IEnumerable<ProductForOrderModel>>
{
    private readonly IProductAppService _productAppService;
    private readonly IInventoryAppService _inventoryAppService;
    private readonly IPictureAppService _pictureAppService;
    private readonly IMediator _mediator;

    public GetProductsByIdsForOrderHandler(IProductAppService productAppService, IInventoryAppService inventoryAppService, IPictureAppService pictureAppService, IMediator mediator)
    {
        _productAppService = productAppService;
        _inventoryAppService = inventoryAppService;
        _pictureAppService = pictureAppService;
        _mediator = mediator;
    }

    public async Task<IEnumerable<ProductForOrderModel>> Handle(GetProductsByIdsForOrderQuery request, CancellationToken cancellationToken)
    {
        if (!request.Ids.Any())
            return [];

        var products = await _productAppService.GetProductsByIdsAsync(request.Ids);

        var vendorOptions = await _mediator.Send(new GetVendorOptionListQuery(), cancellationToken).ConfigureAwait(false);
        var categoryOptions = await _mediator.Send(new GetCategoryOptionListQuery
        {
            BreadcrumbOpts = new BreadcrumbOptions
            {
                Disabled = true
            }
        }, cancellationToken).ConfigureAwait(false);
        var unitMeasurementList = await _mediator.Send(new GetUnitMeasurementListQuery { Keywords = null, PageIndex = 0, PageSize = int.MaxValue }, cancellationToken).ConfigureAwait(false);
        var unitMeasurementById = unitMeasurementList.Data.ToDictionary(u => u.Id);
        var warehouseOptions = await _mediator.Send(new GetWarehouseOptionListQuery(), cancellationToken).ConfigureAwait(false);

        var model = new List<ProductForOrderModel>(products.Count());
        foreach (var productInfo in products)
        {
            var productModel = new ProductForOrderModel(productInfo.Id)
            {
                Name = productInfo.Name,
                CurrentUnitPrice = productInfo.UnitPrice,
                PictureUrl = PictureHelper.GetPictureUrl(productInfo.Pictures?.FirstOrDefault())
            };

            var stockInfo = await _mediator.Send(new GetProductStockInfoQuery(productInfo.Id, default), cancellationToken).ConfigureAwait(false);
            productModel.QuantityOnHand = stockInfo.QuantityOnHand;
            productModel.QuantityReserved = stockInfo.QuantityReserved;
            productModel.QuantityAvailable = stockInfo.QuantityAvailable;
            productModel.AvailableWarehouses = warehouseOptions.Where(option => stockInfo.AvailableWarehouseIds.Contains(option.Id)).ToList();
            if (productInfo.Vendors != null)
                productModel.AvailableVendors = vendorOptions.Where(option => productInfo.Vendors.Any(v => v.VendorId == option.Id)).ToList();
            if (productInfo.UnitMeasurementId.HasValue && unitMeasurementById.TryGetValue(productInfo.UnitMeasurementId.Value, out var um))
            {
                productModel.UnitMeasurement = um.Name;
                productModel.QuantityDecimalPlaces = um.DecimalPlaces;
            }

            model.Add(productModel);
        }

        return model;
    }
}
