using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class ReceivePurchaseOrderItemModel
{
    public Guid PurchaseOrderId { get; set; }
    public Guid PurchaseOrderItemId { get; set; }

    [Display(Name = "Số lượng")]
    public decimal ReceivedQuantity { get; set; }

    [ValidateNever]
    public decimal RemainingQuantity { get; set; }

    [Display(Name = "Kho hàng")]
    public Guid? WarehouseId { get; set; }

    [ValidateNever]
    public bool WarehouseRequired { get; set; }
}
