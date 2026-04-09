using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetProductsByIdsForOrderQuery : IRequest<IEnumerable<ProductForOrderModel>>
{
    public required IEnumerable<Guid> Ids { get; set; }
}
