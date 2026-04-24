using MediatR;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Contracts.Queries.Models.GoodsReceipts;

[Serializable]
public sealed class GetGoodsReceiptListQuery : IRequest<GoodsReceiptListModel>
{
    public string? Keywords { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
