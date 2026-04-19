using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class AddPurchaseOrderItemModel
{
    public Guid PurchaseOrderId { get; set; }

    [Display(Name = "Hàng hóa")]
    public Guid? ProductId { get; set; }

    [Display(Name = "Số lượng")]
    public decimal? Quantity { get; set; }

    [Display(Name = "Đơn giá")]
    public decimal? UnitCost { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [Display(Name = "Kho hàng")]
    public Guid? WarehouseId { get; set; }
}
