using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class EditPurchaseOrderItemModel
{
    [Required]
    public Guid PurchaseOrderId { get; set; }

    [Required]
    public Guid PurchaseOrderItemId { get; set; }

    [Display(Name = "Số lượng")]
    public decimal? Quantity { get; set; }

    [Display(Name = "Đơn giá")]
    public decimal? UnitCost { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}
