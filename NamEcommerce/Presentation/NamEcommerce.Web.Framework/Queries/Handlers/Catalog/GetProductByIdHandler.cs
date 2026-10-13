using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductByIdHandler(IProductAppService productAppService) : IRequestHandler<GetProductByIdQuery, ProductModel?>
{
    public async Task<ProductModel?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await productAppService.GetProductByIdAsync(request.Id).ConfigureAwait(false);
        if (product is null)
            return null;

        var model = new ProductModel
        {
            Id = product.Id,
            Name = product.Name,
            ShortDesc = product.ShortDesc,
            UnitPrice = product.UnitPrice,
            CostPrice = product.CostPrice,
            PictureId = product.Pictures.FirstOrDefault() is Guid pid && pid != Guid.Empty ? pid : null,
            UnitMeasurementId = product.UnitMeasurementId,
            UnitMeasurement = product.UnitMeasurement,
            QuantityDecimalPlaces = product.QuantityDecimalPlaces ?? 2 
        };

        var productCategory = product.Categories.FirstOrDefault();
        model.CategoryId = productCategory?.CategoryId;
        model.DisplayOrder = productCategory?.DisplayOrder ?? 1;

        model.VendorIds = product.Vendors.Select(v => v.VendorId).ToList();

        return model;
    }
}
