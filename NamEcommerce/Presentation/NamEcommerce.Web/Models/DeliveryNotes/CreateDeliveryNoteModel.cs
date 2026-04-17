using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.DeliveryNotes;

[Serializable]
public sealed class CreateDeliveryNoteModel
{
    public Guid OrderId { get; set; }

    [ValidateNever]
    public string OrderCode { get; set; } = string.Empty;
    [ValidateNever]
    public string? OrderNote { get; set; }
    [ValidateNever]
    public string CustomerName { get; set; } = string.Empty;
    
    [Display(Name = "Địa chỉ giao hàng (*)", Prompt = "Nhập địa chỉ giao hàng")]
    [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc.")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Display(Name = "Kho xuất hàng (*)")]
    [Required(ErrorMessage = "Kho xuất hàng là bắt buộc.")]
    public Guid WarehouseId { get; set; }

    [ValidateNever]
    public EntityOptionListModel? AvailableWarehouses { get; set; }
    
    [Display(Name = "Hiển thị giá trên phiếu xuất")]
    public bool ShowPrice { get; set; }
    
    [Display(Name = "Ghi chú phiếu xuất", Prompt = "Nhập ghi chú...")]
    public string? Note { get; set; }

    [Display(Name = "Phụ phí")]
    public decimal Surcharge { get; set; }

    [Display(Name = "Lý do phụ phí", Prompt = "Nhập lý do thu phụ phí...")]
    public string? SurchargeReason { get; set; }

    [Display(Name = "Số tiền phải thu (mặc định = tổng tiền + phụ phí)")]
    public decimal AmountToCollect { get; set; }

    public IList<CreateDeliveryNoteItemModel> Items { get; set; } = [];
}

[Serializable]
public sealed class CreateDeliveryNoteItemModel
{
    public Guid OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal PreviouslyDeliveredQuantity { get; set; }
    
    // Default to the remaining un-delivered quantity
    public decimal Quantity { get; set; }
    
    public decimal UnitPrice { get; set; }
    public bool Selected { get; set; }
}
