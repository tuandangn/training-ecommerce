using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

[Serializable]
public sealed class GetPurchaseOrderListQuery : IRequest<PurchaseOrderListModel>
{
    public string? Keywords { get; init; }
    public int? Status { get; set; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
}
