using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

/// <summary>Lấy danh sách phiếu nhận hàng gắn với một đơn nhập.</summary>
[Serializable]
public sealed class GetRelatedGoodsReceiptsByPurchaseOrderQuery : IRequest<IList<RelatedGoodsReceiptModel>>
{
    public required Guid PurchaseOrderId { get; init; }
}

/// <summary>Lấy danh sách phiếu trả hàng NCC gắn với một đơn nhập.</summary>
[Serializable]
public sealed class GetRelatedVendorReturnsByPurchaseOrderQuery : IRequest<IList<RelatedVendorReturnModel>>
{
    public required Guid PurchaseOrderId { get; init; }
}
