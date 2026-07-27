using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.FastSales;

[Serializable]
public sealed class OrderQuickCreateModel
{
    [Display(Name = "Khách hàng")]
    public Guid? CustomerId { get; set; }
    [ValidateNever]
    public string? CustomerDisplayName { get; set; }
    [ValidateNever]
    public string? CustomerDisplayPhone { get; set; }
    [ValidateNever]
    public string? CustomerDisplayAddress { get; set; }
    [ValidateNever]
    public int CustomerDisplayKind { get; set; }
    [ValidateNever]
    public bool CustomerDisplayIsSystem { get; set; }

    public IList<QuickCreateOrderItemModel> Items { get; set; } = [];

    public bool PayNow { get; set; }
    public bool DeliveryNow { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? ShippingAddress { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? ShippingPhoneNumber { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [ValidateNever]
    public bool BankTransferEnabled { get; set; }
    [ValidateNever]
    public string? BankAccountLabel { get; set; }
    [ValidateNever]
    public bool ManualBankTransferConfirmEnabled { get; set; }
}

[Serializable]
public sealed class OrderQuickCreatePaymentModel
{
    public decimal? PaidAmount { get; set; }
    public Guid? PaymentIntentId { get; set; }
    public decimal? OrderDiscount { get; set; }
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
    public Guid? WarehouseId { get; set; }
    public IEnumerable<ProductWarehouseStockModel> AvailableWarehouseStocks { get; set; } = [];
    public decimal QuantityAvailable { get; set; }

    [ValidateNever]
    public string? UnitMeasurement { get; set; }
    [ValidateNever]
    public int QuantityDecimalPlaces { get; set; }

    [Display(Name = "Số lượng")]
    public decimal? Quantity { get; set; }

    [Display(Name = "Đơn giá")]
    public decimal? UnitPrice { get; set; }

    [ValidateNever]
    public decimal ItemSubTotal => (UnitPrice ?? 0) * (Quantity ?? 0);

    [Serializable]
    public sealed record ProductWarehouseStockModel
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required decimal QuantityOnHand { get; init; }
        public required decimal QuantityReserved { get; init; }
        public required decimal QuantityAvailable { get; init; }
    }
}
