using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetCategoryBreadcrumb : IRequest<string>
{
    public Guid CategoryId { get; set; }

    public BreadcrumbOptions BreadcrumbOpts { get; set; }
}
