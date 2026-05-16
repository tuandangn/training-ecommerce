using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

[Serializable]
public sealed class GetAllocatedPurchaseOrdersForOrderQuery : IRequest<OrderAllocatedPurchaseOrderListModel>
{
    public required Guid OrderId { get; init; }
}
