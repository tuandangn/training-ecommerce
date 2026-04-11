using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed record DeleteOrderItemCommand : IRequest<CommonResultModel>
{
    public required Guid OrderId { get; init; }
    public required Guid ItemId { get; init; }
}
