using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Queries.Models.Returns;

[Serializable]
public sealed class GetVendorReturnQuery : IRequest<VendorReturnModel?>
{
    public required Guid Id { get; init; }
}
