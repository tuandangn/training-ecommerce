using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetCategoryListQuery : IRequest<CategoryListModel>
{
    public required string? Keywords { get; init; }

    public required int PageIndex { get; init; }
    public required int PageSize { get; init; }

    public BreadcrumbOptions BreadcrumbOpts { get; set; }
}
