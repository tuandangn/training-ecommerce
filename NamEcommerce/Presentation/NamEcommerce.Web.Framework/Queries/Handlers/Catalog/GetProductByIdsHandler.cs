using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductByIdsHandler(IProductAppService productAppService) : IRequestHandler<GetProductsByIdsQuery, IEnumerable<ProductModel>>
{
    public async Task<IEnumerable<ProductModel>> Handle(GetProductsByIdsQuery request, CancellationToken cancellationToken)
    {
        if (!request.Ids.Any())
            return Enumerable.Empty<ProductModel>();

        var products = await productAppService.GetProductsByIdsAsync(request.Ids).ConfigureAwait(false);
        var productModels = products.Select(product =>
        {
            var productModel = new ProductModel
            {
                Id = product.Id,
                Name = product.Name,
                ShortDesc = product.ShortDesc,
                UnitPrice = product.UnitPrice,
                CostPrice = product.CostPrice,
                UnitMeasurementId = product.UnitMeasurementId,
                PictureId = product.Pictures.FirstOrDefault() is Guid pid && pid != Guid.Empty ? pid : null
            };

            var productCategory = product.Categories.FirstOrDefault();
            productModel.CategoryId = productCategory?.CategoryId;
            productModel.DisplayOrder = productCategory?.DisplayOrder ?? 1;

            productModel.VendorIds = product.Vendors.Select(v => v.VendorId).ToList();

            return productModel;
        }).ToList();

        return productModels;
    }
}
