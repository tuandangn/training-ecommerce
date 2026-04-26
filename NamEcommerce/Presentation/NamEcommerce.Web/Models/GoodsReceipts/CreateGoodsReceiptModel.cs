using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.GoodsReceipts;

[Serializable]
public sealed class CreateGoodsReceiptModel
{
    [Display(Name = "Ngày nhập hàng")]
    public DateTime CreatedOn { get; set; } = DateTime.Now;

    [Display(Name = "Tên tài xế")]
    public string? TruckDriverName { get; set; }

    [Display(Name = "Biển số xe")]
    public string? TruckNumberSerial { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [ValidateNever]
    [Display(Name = "Kho hàng")]
    public Guid? WarehouseId { get; set; }
    [ValidateNever]
    public EntityOptionListModel? AvailableWarehouses { get; set; }

    /// <summary>
    /// Nhà cung cấp (optional) — có thể gắn lúc tạo hoặc bỏ qua, gắn sau ở Details.
    /// Khi định giá đủ tất cả items + có vendor → tự động sinh VendorDebt.
    /// </summary>
    [ValidateNever]
    [Display(Name = "Nhà cung cấp")]
    public Guid? VendorId { get; set; }

    /// <summary>Snapshot tên NCC để hiển thị lại sau khi ModelState fail.</summary>
    [ValidateNever]
    public string? VendorDisplayName { get; set; }
    [ValidateNever]
    public string? VendorDisplayPhone { get; set; }
    [ValidateNever]
    public string? VendorDisplayAddress { get; set; }

    public IList<Guid> PictureIds { get; set; } = [];

    public IList<CreateGoodsReceiptItemModel> Items { get; set; } = [];
}

[Serializable]
public sealed class CreateGoodsReceiptItemModel
{
    [Display(Name = "Hàng hóa")]
    public Guid? ProductId { get; set; }

    [ValidateNever]
    public string? ProductDisplayName { get; set; }

    [ValidateNever]
    public string? ProductDisplayPicture { get; set; }

    [Display(Name = "Kho")]
    public Guid? WarehouseId { get; set; }

    [ValidateNever]
    public string? WarehouseDisplayName { get; set; }

    [Display(Name = "Số lượng")]
    public decimal Quantity { get; set; }

    [Display(Name = "Đơn giá nhập")]
    public decimal? UnitCost { get; set; }
}
