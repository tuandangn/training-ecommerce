using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetCategoryQuery : IRequest<CategoryModel?>
{
    public required Guid Id { get; init; }
    public BreadcrumbOptions BreadcrumbOpts { get; set; }
}
