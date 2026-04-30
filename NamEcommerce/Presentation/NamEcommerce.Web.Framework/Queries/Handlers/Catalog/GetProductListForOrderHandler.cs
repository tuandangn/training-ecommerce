using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductListForOrderHandler : IRequestHandler<GetProductListForOrderQuery, ProductListForOrderModel>
{
    private readonly IProductAppService _productAppService;
    private readonly IVendorAppService _vendorAppService;
    private readonly IPictureAppService _pictureAppService;
    private readonly IMediator _mediator;
    private readonly IInventoryAppService _inventoryAppService;

    public GetProductListForOrderHandler(IProductAppService productAppService, IVendorAppService vendorAppService, IMediator mediator, IPictureAppService pictureAppService, IInventoryAppService inventoryAppService)
    {
        _productAppService = productAppService;
        _vendorAppService = vendorAppService;
        _mediator = mediator;
        _pictureAppService = pictureAppService;
        _inventoryAppService = inventoryAppService;
    }

    public async Task<ProductListForOrderModel> Handle(GetProductListForOrderQuery request, CancellationToken cancellationToken)
    {
        var products = await _productAppService.GetProductsAsync(0, int.MaxValue, request.Keywords, request.CategoryId, request.VendorId).ConfigureAwait(false);

        var allVendorOptions = await _mediator.Send(new GetVendorOptionListQuery(), cancellationToken).ConfigureAwait(false);
        var allCategoryOptions = await _mediator.Send(new GetCategoryOptionListQuery(), cancellationToken).ConfigureAwait(false);

        var productListItems = new List<ProductListForOrderModel.ProductItemModel>();
        foreach (var productInfo in products)
        {
            var firstCategoryId = productInfo.Categories.FirstOrDefault()?.CategoryId;
            var categoryName = firstCategoryId.HasValue
                ? allCategoryOptions.Options.FirstOrDefault(c => c.Id == firstCategoryId.Value)?.Name
                : null;

            var productModel = new ProductListForOrderModel.ProductItemModel(productInfo.Id)
            {
                Name = productInfo.Name,
                UnitPrice = productInfo.UnitPrice,
                CategoryName = categoryName
            };

            if (productInfo.Pictures.Any())
            {
                var pictureId = productInfo.Pictures.First();
                var base64PictureInfo = await _pictureAppService.GetBase64PictureByIdAsync(pictureId).ConfigureAwait(false);
                productModel.PictureUrl = base64PictureInfo?.Base64Value;
            }

            var stockItems = await _inventoryAppService.GetInventoryStocksForProductAsync(productInfo.Id, request.WarehouseId);
            productModel.QuantityOnHand = stockItems.Sum(item => item.QuantityOnHand);
            productModel.QuantityReserved = stockItems.Sum(item => item.QuantityReserved);
            productModel.QuantityAvailable = stockItems.Sum(item => item.QuantityAvailable);
            productModel.AvailableWarehouseIds = stockItems.Where(item => item.WarehouseId.HasValue).Select(item => item.WarehouseId).OfType<Guid>().ToList();

            if (!request.VendorId.HasValue && productInfo.Vendors != null)
            {
                productModel.AvailableVendors = productInfo.Vendors
                    .Select(v => allVendorOptions.Options.FirstOrDefault(o => o.Id == v.VendorId))
                    .Where(o => o != null)
                    .Select(o => new ProductListForOrderModel.VendorOptionModel(o!.Id, o.Name))
                    .ToList();
            }

            productListItems.Add(productModel);
        }

        var filteredByVendorName = request.VendorId.HasValue
            ? allVendorOptions.FirstOrDefault(v => v.Id == request.VendorId.Value)?.Name
            : null;
        return new ProductListForOrderModel
        {
            Keywords = request.Keywords,
            FilteredByVendorName = filteredByVendorName,
            Data = PagedDataModel.Create(productListItems)
        };
    }
}
