using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed class CreatePurchaseOrderModel
{
    [Display(Name = "Nhà cung cấp")]
    public Guid? VendorId { get; set; }
    [ValidateNever]
    public string? VendorName { get; set; }
    [ValidateNever]
    public string? VendorPhone { get; set; }
    [ValidateNever]
    public string? VendorAddress { get; set; }
    [ValidateNever]
    public bool VendorIsLocked { get; set; }

    [Display(Name = "Kho nhập hàng")]
    public Guid? WarehouseId { get; set; }
    [ValidateNever]
    public required EntityOptionListModel AvailableWarehouses { get; set; }

    [Display(Name = "Ngày giao dự kiến")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IList<CreatePurchaseOrderItemModel> Items { get; set; } = [];

    [ValidateNever]
    public decimal OrderSubTotal => Items.Sum(item => item.ItemSubTotal);
    [ValidateNever]
    public decimal OrderTotal => OrderSubTotal;
}

[Serializable]
public sealed class CreatePurchaseOrderItemModel
{
    [Display(Name = "Hàng hóa")]
    public Guid? ProductId { get; set; }
    [ValidateNever]
    public string? ProductDisplayName { get; set; }
    [ValidateNever]
    public string? ProductDisplayPicture { get; set; }

    [Display(Name = "Số lượng")]
    [UIHint("Quantity")]
    public decimal? Quantity { get; set; }

    [Display(Name = "Đơn giá")]
    [UIHint("Currency")]
    public decimal? UnitCost { get; set; }

    [ValidateNever]
    public decimal ItemSubTotal => (UnitCost ?? 0) * (Quantity ?? 0);
}
