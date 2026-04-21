using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed class AddOrderItemCommand : IRequest<CommonActionResultModel>
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}
