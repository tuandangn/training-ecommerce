using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetProductPurchasePriceReferenceQuery : IRequest<ProductPurchasePriceReferenceModel>
{
    public required Guid ProductId { get; init; }
    public Guid? VendorId { get; init; }
}
