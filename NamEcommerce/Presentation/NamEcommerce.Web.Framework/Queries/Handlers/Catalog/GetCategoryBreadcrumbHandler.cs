using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetCategoryBreadcrumbHandler : IRequestHandler<GetCategoryBreadcrumb, string>
{
    private readonly ICategoryAppService _categoryAppService;

    public GetCategoryBreadcrumbHandler(ICategoryAppService categoryAppService)
    {
        _categoryAppService = categoryAppService;
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
        breadcrumb = string.Join($" {request.BreadcrumbOpts.Separator} ", breadcrumbItems.Select(item => item.Name));

        return breadcrumb;
    }
}
