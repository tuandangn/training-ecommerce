using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed record UpdateOrderItemCommand : IRequest<CommonActionResultModel>
{
    public required Guid OrderId { get; init; }
    public required Guid ItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}
