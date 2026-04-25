using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.GoodsReceipts;

/// <summary>
/// Model để thiết lập đơn giá nhập cho một item trong phiếu nhập hàng.
/// Chỉ áp dụng cho item chưa có đơn giá (IsPendingCosting = true).
/// </summary>
[Serializable]
public sealed class SetGoodsReceiptItemUnitCostModel
{
    public Guid GoodsReceiptId { get; set; }
    public Guid ItemId { get; set; }

    [Display(Name = "Đơn giá nhập")]
    public decimal UnitCost { get; set; }
}
