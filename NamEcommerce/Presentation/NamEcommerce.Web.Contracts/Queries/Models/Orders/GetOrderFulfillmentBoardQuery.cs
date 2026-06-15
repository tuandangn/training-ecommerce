using MediatR;
using NamEcommerce.Web.Contracts.Models.Orders;

namespace NamEcommerce.Web.Contracts.Queries.Models.Orders;

[Serializable]
public sealed record GetOrderFulfillmentBoardQuery : IRequest<OrderFulfillmentBoardModel>
{
    public string? Range { get; init; }
    public DateTime? Date { get; init; }
    public string? Keywords { get; init; }
    public string? Risk { get; init; }
    public bool IncludeInactive { get; init; }
}
