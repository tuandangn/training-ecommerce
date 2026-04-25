using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.GoodsReceipts;

/// <summary>
/// Model cho phép chỉnh sửa thông tin cơ bản và ảnh chứng từ của phiếu nhập hàng.
/// Không được phép thay đổi danh sách hàng hóa (Items).
/// </summary>
[Serializable]
public sealed class EditGoodsReceiptModel
{
    public Guid Id { get; set; }

    [Display(Name = "Ngày nhập hàng")]
    public DateTime CreatedOn { get; set; }

    [Display(Name = "Tên tài xế")]
    public string? TruckDriverName { get; set; }

    [Display(Name = "Biển số xe")]
    public string? TruckNumberSerial { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}
