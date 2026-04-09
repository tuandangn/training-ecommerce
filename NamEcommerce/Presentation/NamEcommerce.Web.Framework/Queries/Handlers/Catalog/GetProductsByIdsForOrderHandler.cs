using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductsByIdsForOrderHandler : IRequestHandler<GetProductsByIdsForOrderQuery, IEnumerable<ProductForOrderModel>>
{
    private readonly IProductAppService _productAppService;
    private readonly IInventoryAppService _inventoryAppService;
    private readonly IPictureAppService _pictureAppService;

    public GetProductsByIdsForOrderHandler(IProductAppService productAppService, IInventoryAppService inventoryAppService, IPictureAppService pictureAppService)
    {
        _productAppService = productAppService;
        _inventoryAppService = inventoryAppService;
        _pictureAppService = pictureAppService;
    }

    public async Task<IEnumerable<ProductForOrderModel>> Handle(GetProductsByIdsForOrderQuery request, CancellationToken cancellationToken)
    {
        var products = await _productAppService.GetProductsByIdsAsync(request.Ids);

        var model = new List<ProductForOrderModel>(products.Count());
        foreach (var productInfo in products)
        {
            var productModel = new ProductForOrderModel(productInfo.Id)
            {
                Name = productInfo.Name
            };

            if (productInfo.Pictures.Any())
            {
                var pictureId = productInfo.Pictures.First();
                var base64PictureInfo = await _pictureAppService.GetBase64PictureByIdAsync(pictureId).ConfigureAwait(false);
                productModel.PictureUrl = base64PictureInfo?.Base64Value;
            }

            var stockItems = await _inventoryAppService.GetInventoryStocksForProductAsync(productInfo.Id, warehouseId: null);
            productModel.QuantityOnHand = stockItems.Sum(item => item.QuantityOnHand);
            productModel.QuantityReserved = stockItems.Sum(item => item.QuantityReserved);
            productModel.QuantityAvailable = stockItems.Sum(item => item.QuantityAvailable);
            productModel.AvailableWarehouseIds = stockItems.Where(item => item.WarehouseId.HasValue).Select(item => item.WarehouseId).OfType<Guid>().ToList();

            model.Add(productModel);
        }

        return model;
    }
}
