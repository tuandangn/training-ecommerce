using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.FastSales;

[Serializable]
public sealed class FastSaleModel
{
    [Display(Name = "Kho mặc định")]
    public Guid WarehouseId { get; set; }
    public IEnumerable<EntityOptionListModel.EntityOptionModel> Warehouses { get; init; } = [];

    [Display(Name = "Khách hàng")]
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerAddress { get; init; }
    public Guid? DefaultCustomerId { get; init; }

    [Display(Name = "Giảm giá")]
    public decimal OrderDiscount { get; set; }

    [Display(Name = "Địa chỉ giao hàng")]
    public string? ShippingAddress { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public bool BankTransferEnabled { get; init; }
    public string? BankAccountLabel { get; init; }
    public bool ManualBankTransferConfirmEnabled { get; init; }
}
