using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductModel?>
{
    private readonly IProductAppService _productAppService;
    private readonly IPictureAppService _pictureAppService;

    public GetProductByIdHandler(IProductAppService productAppService, IPictureAppService pictureAppService)
    {
        _productAppService = productAppService;
        _pictureAppService = pictureAppService;
    }

    public async Task<ProductModel?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var productInfo = await _productAppService.GetProductByIdAsync(request.Id);
        if (productInfo is null)
            return null;

        var model = new ProductModel
        {
            Id = productInfo.Id,
            Name = productInfo.Name,
            ShortDesc = productInfo.ShortDesc
        };

        var productCategory = productInfo.Categories.FirstOrDefault();
        model.CategoryId = productCategory?.CategoryId;
        model.DisplayOrder = productCategory?.DisplayOrder ?? 1;

        if (productInfo.Pictures.Any())
        {
            var pictureInfo = await _pictureAppService.GetBase64PictureByIdAsync(productInfo.Pictures.First()).ConfigureAwait(false);
            if (pictureInfo is not null)
            {
                model.ImageFile = new Base64ImageModel
                {
                    Base64Data = pictureInfo.Base64Value,
                    Extension = pictureInfo.Extension,
                    FileName = pictureInfo.FileName
                };
            }
        }

        return model;
    }
}
