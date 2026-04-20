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

    /// <summary>
    /// Giá bán mới (tùy chọn). Nếu null hoặc không nhập thì giữ nguyên giá bán hiện tại của sản phẩm.
    /// </summary>
    [Display(Name = "Giá bán")]
    public decimal? SellingPrice { get; set; }

    /// <summary>
    /// Giá bán hiện tại của sản phẩm — dùng để pre-fill input và so sánh cảnh báo trên UI.
    /// </summary>
    [ValidateNever]
    public decimal CurrentUnitPrice { get; set; }

    /// <summary>
    /// Giá vốn của dòng nhập này — dùng để cảnh báo khi giá bán &lt;= giá vốn.
    /// </summary>
    [ValidateNever]
    public decimal UnitCost { get; set; }
}
