using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class EditPurchaseOrderModel
{
    public Guid Id { get; set; }

    [Display(Name = "Ngày đặt")]
    public DateTime PlacedOn { get; set; }

    [Display(Name = "Nhà cung cấp")]
    public Guid VendorId { get; set; }
    [ValidateNever]
    public string? VendorName { get; set; }
    [ValidateNever]
    public string? VendorPhone { get; set; }
    [ValidateNever]
    public string? VendorAddress { get; set; }

    public Guid? WarehouseId { get; set; }
    [ValidateNever]
    public required EntityOptionListModel AvailableWarehouses { get; set; }

    [Display(Name = "Ngày nhập dự kiến")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [Display(Name = "Tổng thuế")]
    public decimal? TaxAmount { get; set; }

    [Display(Name = "Phí vận chuyển")]
    public decimal? ShippingAmount { get; set; }

    [ValidateNever]
    public bool CanChangeDate { get; set; }
    [ValidateNever]
    public bool CanChangeFees { get; set; }
    [ValidateNever]
    public bool CanChangeVendor { get; set; }

    [ValidateNever]
    public decimal TotalAmount { get; set; }
    [ValidateNever]
    public DateTime CreatedOn { get; set; }
}
