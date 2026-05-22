using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Queries.Models.Returns;

/// <summary>
/// Lấy danh sách items có thể trả của một phiếu nhập kho — bao gồm số lượng đã trả từ các phiếu Confirmed khác.
/// </summary>
[Serializable]
public sealed class GetGoodsReceiptItemsForReturnQuery : IRequest<List<ReturnableItemModel>>
{
    public required Guid GoodsReceiptId { get; init; }

    /// <summary>Bỏ qua phiếu trả NCC này khi tính AlreadyReturnedQty (dùng khi đang edit).</summary>
    public Guid? ExcludeReturnId { get; init; }
}
