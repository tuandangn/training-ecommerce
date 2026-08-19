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

    [Display(Name = "Giá bán")]
    public decimal? SellingPrice { get; set; }

    [ValidateNever]
    public decimal CurrentUnitPrice { get; set; }
    [ValidateNever]
    public decimal UnitCost { get; set; }

    public decimal? TaxRate { get; set; }

    public Guid? DirectShipOrderId { get; set; }
    public Guid? DirectShipOrderItemId { get; set; }
    public Guid? DirectShipExistingAllocationId { get; set; }
    public string? DirectShipAddress { get; set; }
    public string? DirectShipContactName { get; set; }
    public string? DirectShipContactPhone { get; set; }
}
