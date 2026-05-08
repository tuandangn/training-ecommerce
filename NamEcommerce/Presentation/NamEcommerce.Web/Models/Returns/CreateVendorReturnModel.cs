using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Returns;

[Serializable]
public sealed class CreateVendorReturnModel
{
    [Display(Name = "Nhà cung cấp")]
    public Guid? VendorId { get; set; }

    [ValidateNever]
    public string? VendorDisplayName { get; set; }

    [Display(Name = "Đơn đặt hàng")]
    public Guid? PurchaseOrderId { get; set; }

    [ValidateNever]
    public string? PurchaseOrderDisplayCode { get; set; }

    [Display(Name = "Phiếu nhập kho")]
    public Guid? GoodsReceiptId { get; set; }

    [Display(Name = "Kho xuất hàng trả")]
    public Guid? WarehouseId { get; set; }

    [ValidateNever]
    public EntityOptionListModel? AvailableWarehouses { get; set; }

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

    [Display(Name = "Giá vốn")]
    public decimal UnitCost { get; set; }
}
