using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.FastSales;

[Serializable]
public sealed class OrderQuickCreateModel
{
    [Display(Name = "Khách hàng")]
    public Guid CustomerId { get; set; }

    public IList<QuickCreateOrderItemModel> Items { get; set; } = [];

    [Display(Name = "Giảm giá")]
    public decimal? OrderDiscount { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? ShippingAddress { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? ShippingPhoneNumber { get; set; }

    [Display(Name = "Thanh toán")]
    public decimal? PaidAmount { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [ValidateNever]
    public bool BankTransferEnabled { get; init; }
    [ValidateNever]
    public string? BankAccountLabel { get; init; }
    [ValidateNever]
    public bool ManualBankTransferConfirmEnabled { get; init; }
}

[Serializable]
public sealed class QuickCreateOrderItemModel
{
    [Display(Name = "Hàng hóa")]
    public Guid? ProductId { get; set; }
    [ValidateNever]
    public string? ProductDisplayName { get; set; }
    [ValidateNever]
    public string? ProductDisplayPicture { get; set; }
    [ValidateNever]
    public decimal? ProductDisplayQty { get; set; }

    [Display(Name = "Kho hàng")]
    public Guid WarehouseId { get; set; }

    [ValidateNever]
    public int QuantityDecimalPlaces { get; set; }

    [Display(Name = "Số lượng")]
    public decimal? Quantity { get; set; }

    [Display(Name = "Đơn giá")]
    public decimal? UnitPrice { get; set; }

    [ValidateNever]
    public decimal ItemSubTotal => (UnitPrice ?? 0) * (Quantity ?? 0);
}

