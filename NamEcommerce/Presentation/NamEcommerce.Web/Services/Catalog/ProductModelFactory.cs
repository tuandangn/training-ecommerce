using MediatR;
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
        var categoryOptions = await _mediator.Send(new GetCategoryOptionListQuery()).ConfigureAwait(false);
        var unitMeasurementOptions = await _mediator.Send(new GetUnitMeasurementOptionListQuery()).ConfigureAwait(false);
        var model = oldModel ?? new CreateProductModel
        {
            DisplayOrder = 1,
            AvailableCategories = categoryOptions,
            AvailableUnitMeasurements = unitMeasurementOptions
        };
        if (oldModel is not null)
        {
            model.AvailableCategories = categoryOptions;
            model.AvailableUnitMeasurements = unitMeasurementOptions;
        }

        return model;
    }

    public async Task<EditProductModel?> PrepareEditProductModel(Guid id, EditProductModel? oldModel = null)
    {
        var product = await _mediator.Send(new GetProductByIdQuery { Id = id }).ConfigureAwait(false);
        if (product is null && oldModel is null)
            return null;

        var categoryOptions = await _mediator.Send(new GetCategoryOptionListQuery()).ConfigureAwait(false);
        var unitMeasurementOptions = await _mediator.Send(new GetUnitMeasurementOptionListQuery()).ConfigureAwait(false);
        var model = oldModel ?? new EditProductModel
        {
            Id = product!.Id,
            Name = product!.Name,
            ShortDesc = product.ShortDesc,
            CategoryId = product.CategoryId,
            AvailableCategories = categoryOptions,
            UnitMeasurementId = product.UnitMeasurementId,
            AvailableUnitMeasurements = unitMeasurementOptions,
            DisplayOrder = product.DisplayOrder,
            ImageFile = product.ImageFile ?? new(),
        };
        if (oldModel is not null)
        {
            model.AvailableCategories = categoryOptions;
            model.AvailableUnitMeasurements = unitMeasurementOptions;
        }

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
        }).ConfigureAwait(false);

        return model;
    }
}
