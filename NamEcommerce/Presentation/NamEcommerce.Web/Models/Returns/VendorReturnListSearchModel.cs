using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.Returns;

[Serializable]
public sealed record VendorReturnListSearchModel : BasePaginationModel
{
    public Guid? VendorId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public int? Status { get; set; }
}
