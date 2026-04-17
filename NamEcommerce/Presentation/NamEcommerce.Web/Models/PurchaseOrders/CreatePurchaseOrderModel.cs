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
    public required EntityOptionListModel AvailableVendors { get; set; }

    [Display(Name = "Nhập vào kho hàng")]
    public Guid? WarehouseId { get; set; }
    [ValidateNever]
    public required EntityOptionListModel AvailableWarehouses { get; set; }

    [Display(Name = "Ngày giao dự kiến")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}
