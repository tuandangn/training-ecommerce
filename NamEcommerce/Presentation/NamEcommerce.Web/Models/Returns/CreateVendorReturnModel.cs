using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Returns;

[Serializable]
public sealed class CreateVendorReturnModel
{
    /// <summary>Nhà cung cấp — dùng VendorPicker component.</summary>
    [Display(Name = "Nhà cung cấp")]
    public Guid? VendorId { get; set; }

    [ValidateNever]
    public string? VendorDisplayName { get; set; }

    /// <summary>Phiếu nhập kho nguồn — null = tạo tự do.</summary>
    [Display(Name = "Phiếu nhập kho")]
    public Guid? GoodsReceiptId { get; set; }

    [ValidateNever]
    public string? GoodsReceiptDisplayLabel { get; set; }

    [Display(Name = "Kho xuất hàng trả")]
    public Guid? WarehouseId { get; set; }

    [ValidateNever]
    public EntityOptionListModel? AvailableWarehouses { get; set; }

    [Display(Name = "Chi phí phát sinh")]
    public decimal AdditionalCost { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IList<CreateVendorReturnItemModel> Items { get; set; } = [];
}

[Serializable]
public sealed class CreateVendorReturnItemModel
{
    [Display(Name = "Hàng hóa")]
    public Guid? ProductId { get; set; }

    [ValidateNever]
    public string? ProductDisplayName { get; set; }

    public Guid? GoodsReceiptItemId { get; set; }

    [Display(Name = "Số lượng yêu cầu")]
    public decimal RequestedQuantity { get; set; }

    [Display(Name = "Số lượng chấp nhận")]
    public decimal AcceptedQuantity { get; set; }

    [Display(Name = "Giá vốn gốc")]
    public decimal? OriginalUnitCost { get; set; }

    [Display(Name = "Giá trả NCC")]
    public decimal ReturnUnitCost { get; set; }
}
