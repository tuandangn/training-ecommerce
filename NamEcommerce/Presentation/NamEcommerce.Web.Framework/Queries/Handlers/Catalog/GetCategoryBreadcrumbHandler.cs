using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetCategoryBreadcrumbHandler : IRequestHandler<GetCategoryBreadcrumb, string>
{
    private readonly ICategoryAppService _categoryAppService;
    private readonly AppConfig _appConfig;

    public GetCategoryBreadcrumbHandler(ICategoryAppService categoryAppService, AppConfig appConfig)
    {
        _categoryAppService = categoryAppService;
        _appConfig = appConfig;
    }

    public async Task<string> Handle(GetCategoryBreadcrumb request, CancellationToken cancellationToken)
    {
        var category = await _categoryAppService.GetCategoryByIdAsync(request.CategoryId);
        if (category is null)
            return string.Empty;

        if (request.BreadcrumbOpts.Disabled)
            return category.Name;

        string? breadcrumb = null;
        var breadcrumbItems = await _categoryAppService.GetCategoryBreadcrumbAsync(category.Id);
        if (request.BreadcrumbOpts.ExcludeCurrent)
            breadcrumbItems = breadcrumbItems.Where(c => c.Id != category.Id);
        var separator = string.IsNullOrEmpty(request.BreadcrumbOpts.Separator) ? _appConfig.BreadcrumbSeparator : request.BreadcrumbOpts.Separator;

        breadcrumb = string.Join($" {separator} ", breadcrumbItems.Select(item => item.Name));

        return breadcrumb;
    }
}
