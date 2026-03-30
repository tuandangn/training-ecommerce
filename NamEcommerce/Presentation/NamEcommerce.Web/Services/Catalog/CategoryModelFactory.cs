using MediatR;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Services.Catalog;

public sealed class CategoryModelFactory : ICategoryModelFactory
{
    private readonly IMediator _mediator;
    private readonly AppConfig _appConfig;

    public CategoryModelFactory(IMediator mediator, AppConfig appConfig)
    {
        _mediator = mediator;
        _appConfig = appConfig;
    }

    public async Task<CategoryListModel> PrepareCategoryListModel(CategoryListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetCategoryListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize,
            BreadcrumbOpts = new()
            {
                ExcludeCurrent = true
            }
        }).ConfigureAwait(false);

        return model;
    }

    public async Task<CreateCategoryModel> PrepareCreateCategoryModel(CreateCategoryModel? oldModel = null)
    {
        var parentOptions = await _mediator.Send(new GetCategoryOptionListQuery()).ConfigureAwait(false);
        var model = oldModel ?? new CreateCategoryModel
        {
            DisplayOrder = 1,
            AvailableParents = parentOptions
        };
        if (oldModel is not null)
            model.AvailableParents = parentOptions;

        return model;
    }

    public async Task<EditCategoryModel?> PrepareEditCategoryModel(Guid id, EditCategoryModel? oldModel = null)
    {
        var category = await _mediator.Send(new GetCategoryQuery { Id = id }).ConfigureAwait(false);
        if (category is null && oldModel is null)
            return null;

        var parentOptions = await _mediator.Send(new GetCategoryOptionListQuery
        {
            ExcludedCategoryId = id
        }).ConfigureAwait(false);
        var model = oldModel ?? new EditCategoryModel
        {
            Id = category!.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            DisplayOrder = category.DisplayOrder,
            AvailableParents = parentOptions
        };

        if (oldModel is not null)
            model.AvailableParents = parentOptions;

        return model;
    }
}
