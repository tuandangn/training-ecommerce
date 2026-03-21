using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetVendorQuery : IRequest<VendorModel?>
{
    public required Guid Id { get; init; }
}
