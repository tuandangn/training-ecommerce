using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Returns;

[Serializable]
public sealed class CreateCustomerReturnModel
{
    [Display(Name = "Phiếu xuất kho")]
    public Guid? DeliveryNoteId { get; set; }
    [ValidateNever]
    public string? DeliveryNoteDisplayCode { get; set; }

    [ValidateNever]
    public Guid? CustomerId { get; set; }
    [ValidateNever]
    public string? CustomerDisplayName { get; set; }
    [ValidateNever]
    public string? CustomerDisplayPhone { get; set; }
    [ValidateNever]
    public string? CustomerDisplayAddress { get; set; }

    [Display(Name = "Chi phí phát sinh")]
    public decimal AdditionalCost { get; set; }

    [Display(Name = "Bù vào lần xuất hàng sau")]
    public bool CompensateInNextDelivery { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IList<CreateCustomerReturnItemModel> Items { get; set; } = [];
}

[Serializable]
public sealed class CreateCustomerReturnItemModel
{
    [Display(Name = "Hàng hóa")]
    public Guid? ProductId { get; set; }

    [ValidateNever]
    public string? ProductDisplayName { get; set; }

    [ValidateNever]
    public int QuantityDecimalPlaces { get; set; }

    public Guid? DeliveryNoteItemId { get; set; }

    [Display(Name = "Số lượng yêu cầu")]
    public decimal RequestedQuantity { get; set; }

    [Display(Name = "Số lượng chấp nhận")]
    public decimal AcceptedQuantity { get; set; }

    [Display(Name = "Đơn giá gốc")]
    public decimal? OriginalUnitPrice { get; set; }

    [Display(Name = "Đơn giá trả về")]
    public decimal ReturnUnitPrice { get; set; }
}
