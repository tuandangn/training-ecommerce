using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.GoodsReceipts;

[Serializable]
public sealed record GoodsReceiptListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
