using MediatR;
using NamEcommerce.Web.Contracts.Models.Orders;

namespace NamEcommerce.Web.Contracts.Queries.Models.Orders;

[Serializable]
public sealed class GetOrderByIdQuery : IRequest<OrderModel?>
{
    public Guid Id { get; init; }
}
