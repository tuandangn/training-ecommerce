using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductListHandler : IRequestHandler<GetProductListQuery, ProductListModel>
{
    private readonly IProductAppService _productAppService;
    private readonly IPictureAppService _pictureAppService;
    private readonly IMediator _mediator;

    public GetProductListHandler(IProductAppService productAppService, IMediator mediator, IPictureAppService pictureAppService)
    {
        _productAppService = productAppService;
        _mediator = mediator;
        _pictureAppService = pictureAppService;
    }

    public async Task<ProductListModel> Handle(GetProductListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _productAppService.GetProductsAsync(request.Keywords, false, request.PageIndex, request.PageSize);

        var productListItems = new List<ProductListModel.ProductItemModel>();
        foreach (var productInfo in pagedData)
        {
            var productModel = new ProductListModel.ProductItemModel(productInfo.Id)
            {
                Name = productInfo.Name,
                ShortDesc = productInfo.ShortDesc
            };

            var productCategory = productInfo.Categories.FirstOrDefault();
            if (productCategory != null)
            {
                productModel.CategoryId = productCategory.CategoryId;
                productModel.CategoryBreadcrumb = await _mediator.Send(new GetCategoryBreadcrumb
                {
                    CategoryId = productCategory.CategoryId
                }, cancellationToken).ConfigureAwait(false);
            }

            if (productInfo.Pictures.Any())
            {
                var pictureId = productInfo.Pictures.First();
                var base64PictureInfo = await _pictureAppService.GetBase64PictureByIdAsync(pictureId).ConfigureAwait(false);
                productModel.PictureUrl = base64PictureInfo?.Base64Value;
            }

            productListItems.Add(productModel);
        }

        var model = new ProductListModel
        {
            Keywords = request.Keywords,
            Data = PagedDataModel.Create(productListItems, request.PageIndex, request.PageSize, pagedData.Pagination.TotalCount)
        };

        return model;
    }
}
