using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetProductByIdQuery : IRequest<ProductModel?>
{
    public required Guid Id { get; init; }
}
