using MediatR;
using NamEcommerce.Web.Contracts.Models.Orders;

namespace NamEcommerce.Web.Contracts.Queries.Models.Orders;

[Serializable]
public sealed class GetOrderListQuery : IRequest<OrderListModel>
{
    public string? Keywords { get; init; }
    public int? Status { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
}
