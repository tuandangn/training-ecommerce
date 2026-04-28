using MediatR;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Contracts.Queries.Models.GoodsReceipts;

[Serializable]
public sealed class GetSuggestedPurchaseOrdersForGoodsReceiptQuery : IRequest<IList<SuggestedPurchaseOrderModel>>
{
    public required Guid GoodsReceiptId { get; init; }
}
