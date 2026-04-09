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
    private readonly IPictureAppService _pictureAppService;
    private readonly IMediator _mediator;
    private readonly IInventoryAppService _inventoryAppService;

    public GetProductListForOrderHandler(IProductAppService productAppService, IMediator mediator, IPictureAppService pictureAppService, IInventoryAppService inventoryAppService)
    {
        _productAppService = productAppService;
        _mediator = mediator;
        _pictureAppService = pictureAppService;
        _inventoryAppService = inventoryAppService;
    }

    public async Task<ProductListForOrderModel> Handle(GetProductListForOrderQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _productAppService.GetProductsAsync(request.Keywords, false, 0, int.MaxValue);

        var productListItems = new List<ProductListForOrderModel.ProductItemModel>();
        foreach (var productInfo in pagedData)
        {
            var productModel = new ProductListForOrderModel.ProductItemModel(productInfo.Id)
            {
                Name = productInfo.Name
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

            productListItems.Add(productModel);
        }

        var model = new ProductListForOrderModel
        {
            Keywords = request.Keywords,
            Data = PagedDataModel.Create(productListItems, 0, int.MaxValue, pagedData.Pagination.TotalCount)
        };

        return model;
    }
}
