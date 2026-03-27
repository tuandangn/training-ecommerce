using MediatR;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Services.Catalog;

public sealed class ProductModelFactory : IProductModelFactory
{
    private readonly IMediator _mediator;
    private readonly AppConfig _appConfig;

    public ProductModelFactory(IMediator mediator, AppConfig appConfig)
    {
        _mediator = mediator;
        _appConfig = appConfig;
    }

    public async Task<CreateProductModel> PrepareCreateProductModel(CreateProductModel? oldModel = null)
    {
        var categoryOptions = await _mediator.Send(new GetCategoryOptionListQuery());
        var model = oldModel ?? new CreateProductModel
        {
            DisplayOrder = 1,
            Categories = categoryOptions
        };
        if (oldModel is not null)
            model.Categories = categoryOptions;

        return model;
    }

    public async Task<EditProductModel?> PrepareEditProductModel(Guid id, EditProductModel? oldModel = null)
    {
        var product = await _mediator.Send(new GetProductQuery { Id = id });
        if (product is null && oldModel is null)
            return null;

        var categoryOptions = await _mediator.Send(new GetCategoryOptionListQuery());
        var model = oldModel ?? new EditProductModel
        {
            Id = product!.Id,
            Name = product!.Name,
            ShortDesc = product.ShortDesc,
            Categories = categoryOptions,
            CategoryId = product.CategoryId,
            DisplayOrder = product.DisplayOrder,
            ImageFile = product.ImageFile ?? new(),
        };
        if (oldModel is not null)
            model.Categories = categoryOptions;

        return model;
    }

    public async Task<ProductListModel> PrepareProductListModel(ProductListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetProductListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return model;
    }
}
