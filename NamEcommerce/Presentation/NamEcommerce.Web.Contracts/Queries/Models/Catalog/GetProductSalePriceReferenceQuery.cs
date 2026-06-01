using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetProductSalePriceReferenceQuery : IRequest<ProductSalePriceReferenceModel>
{
    public required Guid ProductId { get; init; }
    public Guid? CustomerId { get; init; }
    public int Take { get; init; } = 10;
}
