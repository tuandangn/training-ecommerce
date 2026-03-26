using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetCategoryOptionListQuery : IRequest<EntityOptionListModel>
{
    public Guid? ExcludedCategoryId { get; set; }
    public BreadcrumbOptions BreadcrumbOpts { get; set; }
}
