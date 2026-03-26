using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog
{
    [Serializable]
    public sealed record DeleteProductCommand(Guid Id) : IRequest<DeleteProductResultModel>;
}
