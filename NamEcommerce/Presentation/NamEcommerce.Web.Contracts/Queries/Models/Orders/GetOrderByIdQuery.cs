using MediatR;
using NamEcommerce.Web.Contracts.Models.Orders;

namespace NamEcommerce.Web.Contracts.Queries.Models.Orders;

[Serializable]
public sealed class GetOrderByIdQuery : IRequest<OrderDetailsModel?>
{
    public Guid Id { get; init; }
}
