using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

[Serializable]
public sealed class GetRelatedGoodsReceiptsByPurchaseOrderQuery : IRequest<IList<RelatedGoodsReceiptModel>>
{
    public required Guid PurchaseOrderId { get; init; }
}

[Serializable]
public sealed class GetRelatedVendorReturnsByPurchaseOrderQuery : IRequest<IList<RelatedVendorReturnModel>>
{
    public required Guid PurchaseOrderId { get; init; }
}
