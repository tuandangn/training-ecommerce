using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetProductsByIdsQuery : IRequest<IEnumerable<ProductModel>>
{
    public required IEnumerable<Guid> Ids { get; init; }
}
