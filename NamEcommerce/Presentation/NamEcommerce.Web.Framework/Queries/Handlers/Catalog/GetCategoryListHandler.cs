using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetCategoryListHandler : IRequestHandler<GetCategoryListQuery, CategoryListModel>
{
    private readonly ICategoryAppService _categoryAppService;
    private readonly IMediator _mediator;

    public GetCategoryListHandler(ICategoryAppService categoryAppService, IMediator mediator)
    {
        _categoryAppService = categoryAppService;
        _mediator = mediator;
    }

    public async Task<CategoryListModel> Handle(GetCategoryListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _categoryAppService.GetCategoriesAsync(request.Keywords, request.PageIndex, request.PageSize);

        var categories = new List<CategoryListModel.ItemModel>();
        foreach (var item in pagedData)
            categories.Add(await toListItemAsync(item));

        var model = new CategoryListModel
        {
            Keywords = request.Keywords,
            BreadcrumbOpts = request.BreadcrumbOpts,
            Data = PagedDataModel.Create(categories, request.PageIndex, request.PageSize, pagedData.Pagination.TotalCount)
        };

        return model;

        //local methods
        async Task<CategoryListModel.ItemModel> toListItemAsync(CategoryAppDto item)
        {
            var breadcrumb = await _mediator.Send(new GetCategoryBreadcrumb
            {
                BreadcrumbOpts = request.BreadcrumbOpts,
                CategoryId = item.Id
            }, cancellationToken).ConfigureAwait(false);

            return new CategoryListModel.ItemModel(item.Id)
            {
                Name = item.Name,
                Breadcrumb = breadcrumb,
                DisplayOrder = item.DisplayOrder,
                ParentId = item.ParentId
            };
        }
    }
}
