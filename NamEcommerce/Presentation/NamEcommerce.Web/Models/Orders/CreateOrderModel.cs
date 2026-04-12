using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class CreateOrderModel
{
    [Display(Name = "Khách hàng")]
    public Guid? CustomerId { get; set; }
    [ValidateNever]
    public string? CustomerDisplayName { get; set; }
    [ValidateNever]
    public string? CustomerDisplayPhone { get; set; }
    [ValidateNever]
    public string? CustomerDisplayAddress { get; set; }

    [Display(Name = "Ngày giao dự kiến")]
    public DateTime? ExpectedShippingDate { get; set; }

    [Display(Name = "Địa chỉ giao hàng")]
    public string? ShippingAddress { get; set; }

    [Display(Name = "Giảm giá")]
    public decimal? OrderDiscount { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IList<CreateOrderItemModel> Items { get; set; } = [];

    [ValidateNever]
    public decimal OrderSubTotal => Items.Sum(item => item.ItemSubTotal);
    [ValidateNever]
    public decimal OrderTotal => OrderSubTotal - (OrderDiscount ?? 0);
}

[Serializable]
public sealed class CreateOrderItemModel
{
    [Display(Name = "Hàng hóa")]
    public Guid? ProductId { get; set; }
    [ValidateNever]
    public string? ProductDisplayName { get; set; }
    [ValidateNever]
    public string? ProductDisplayPicture { get; set; }
    [ValidateNever]
    public decimal? ProductDisplayQty { get; set; }

    [Display(Name = "Số lượng")]
    public decimal? Quantity { get; set; }

    [Display(Name = "Đơn giá")]
    public decimal? UnitPrice { get; set; }

    [ValidateNever]
    public decimal ItemSubTotal => (UnitPrice ?? 0) * (Quantity ?? 0);
}
