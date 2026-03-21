using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog
{
    [Serializable]
    public sealed record DeleteVendorCommand(Guid Id) : IRequest<DeleteVendorResultModel>;
}
