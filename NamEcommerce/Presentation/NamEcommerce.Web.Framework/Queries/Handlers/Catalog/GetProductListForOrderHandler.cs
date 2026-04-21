using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
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
        IEnumerable<Application.Contracts.Dtos.Catalog.ProductAppDto> products;
        string? vendorName = null;

        if (request.VendorId.HasValue)
        {
            products = await _productAppService.GetProductsByVendorIdAsync(request.VendorId.Value).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(request.Keywords))
            {
                var lowerKeywords = request.Keywords.ToLowerInvariant();
                products = products.Where(p => p.Name.ToLowerInvariant().Contains(lowerKeywords));
            }
            var vendor = await _vendorAppService.GetVendorByIdAsync(request.VendorId.Value).ConfigureAwait(false);
            if (vendor != null) vendorName = vendor.Name;
        }
        else
        {
            var pagedData = await _productAppService.GetProductsAsync(request.Keywords, 0, int.MaxValue).ConfigureAwait(false);
            products = pagedData;
        }

        var allVendorOptions = await _mediator.Send(new GetVendorOptionListQuery()).ConfigureAwait(false);

        var productListItems = new List<ProductListForOrderModel.ProductItemModel>();
        foreach (var productInfo in products)
        {
            var productModel = new ProductListForOrderModel.ProductItemModel(productInfo.Id)
            {
                Name = productInfo.Name,
                UnitPrice = productInfo.UnitPrice
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

        return new ProductListForOrderModel
        {
            Keywords = request.Keywords,
            FilteredByVendorName = vendorName,
            Data = PagedDataModel.Create(productListItems)
        };
    }
}
