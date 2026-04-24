using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.GoodsReceipts;

[Serializable]
public sealed class GoodsReceiptListModel
{
    public string? Keywords { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required DateTime CreatedOn { get; init; }
        public string? TruckDriverName { get; init; }
        public string? TruckNumberSerial { get; init; }
        public bool IsPendingCosting { get; init; }
        public int ItemCount { get; init; }
    }
}
