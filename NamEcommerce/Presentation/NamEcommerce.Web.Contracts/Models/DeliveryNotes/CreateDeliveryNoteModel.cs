using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Contracts.Models.DeliveryNotes;

public sealed class CreateDeliveryNoteModel
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    
    [Display(Name = "Địa chỉ giao hàng (*)", Prompt = "Nhập địa chỉ giao hàng")]
    [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc.")]
    public string ShippingAddress { get; set; } = string.Empty;
    
    [Display(Name = "Hiển thị giá trên phiếu xuất")]
    public bool ShowPrice { get; set; }
    
    [Display(Name = "Ghi chú phiếu xuất", Prompt = "Nhập ghi chú...")]
    public string? Note { get; set; }

    public IList<CreateDeliveryNoteItemModel> Items { get; set; } = [];
}

public sealed class CreateDeliveryNoteItemModel
{
    public Guid OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal PreviouslyDeliveredQuantity { get; set; }
    
    // Default to the remaining un-delivered quantity
    public decimal Quantity { get; set; }
    
    public decimal UnitPrice { get; set; }
    public bool Selected { get; set; }
}
