using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

[Serializable]
public sealed class GetPurchaseOrderQuery : IRequest<PurchaseOrderModel?>
{
    public required Guid Id { get; init; }
}
