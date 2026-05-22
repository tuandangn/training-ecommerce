using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductListHandler(
    IProductAppService productAppService, IMediator mediator,
    IPictureAppService pictureAppService, IUnitMeasurementAppService unitMeasurementAppService,
    IVendorAppService vendorAppService) : IRequestHandler<GetProductListQuery, ProductListModel>
{
    public async Task<ProductListModel> Handle(GetProductListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await productAppService.GetProductsAsync(request.PageIndex, request.PageSize, request.Keywords, request.CategoryId, request.VendorId);

        var categoryBreadcrumbs = new Dictionary<Guid, string>();
        var categoryIds = pagedData.Where(product => product.Categories.Any())
            .Select(product => product.Categories.First().CategoryId)
            .Distinct();
        foreach (var categoryId in categoryIds)
        {
            var breadcrumb = await mediator.Send(new GetCategoryBreadcrumb
            {
                CategoryId = categoryId
            }, cancellationToken).ConfigureAwait(false);
            categoryBreadcrumbs[categoryId] = breadcrumb;
        }

        var unitMeasurementIds = pagedData.Where(p => p.UnitMeasurementId.HasValue).Select(p => p.UnitMeasurementId!.Value).Distinct();
        var unitMeasurements = await unitMeasurementAppService.GetUnitMeasurementsByIdsAsync(unitMeasurementIds).ConfigureAwait(false);

        var vendorIds = pagedData.SelectMany(p => p.Vendors.Select(v => v.VendorId)).Distinct();
        var vendors = await vendorAppService.GetVendorsByIdsAsync(vendorIds).ConfigureAwait(false);

        var productModels = new List<ProductListModel.ProductItemModel>();
        foreach (var product in pagedData)
        {
            var productModel = new ProductListModel.ProductItemModel(product.Id)
            {
                Name = product.Name,
                ShortDesc = product.ShortDesc,
                UnitPrice = product.UnitPrice,
            };

            productModel.UnitMeasurementName = unitMeasurements.FirstOrDefault(um => um.Id == product.UnitMeasurementId)?.Name;

            var productCategory = product.Categories.FirstOrDefault();
            if (productCategory != null)
            {
                productModel.CategoryId = productCategory.CategoryId;
                productModel.CategoryBreadcrumb = categoryBreadcrumbs[productCategory.CategoryId];
            }

            if (product.Pictures.Any())
            {
                var pictureId = product.Pictures.First();
                var base64PictureInfo = await pictureAppService.GetBase64PictureByIdAsync(pictureId).ConfigureAwait(false);
                productModel.PictureUrl = base64PictureInfo?.Base64Value;
            }

            var productVendors = vendors.Where(v => product.Vendors.Any(pv => pv.VendorId == v.Id));
            productModel.VendorNames = productVendors.Select(pv => pv.Name);

            var stockInfo = await mediator.Send(new GetProductStockInfoQuery(product.Id, null)).ConfigureAwait(false);
            productModel.StockQuantity = stockInfo.QuantityAvailable;

            productModels.Add(productModel);
        }

        var model = new ProductListModel
        {
            Keywords = request.Keywords,
            Data = PagedDataModel.Create(productModels, request.PageIndex, request.PageSize, pagedData.Pagination.TotalCount)
        };

        return model;
    }
}
