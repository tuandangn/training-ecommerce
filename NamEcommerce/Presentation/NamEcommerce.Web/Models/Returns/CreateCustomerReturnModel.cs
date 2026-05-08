using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Returns;

[Serializable]
public sealed class CreateCustomerReturnModel
{
    [Display(Name = "Đơn hàng")]
    public Guid? OrderId { get; set; }

    [ValidateNever]
    public string? OrderDisplayCode { get; set; }

    [Display(Name = "Kho nhận hàng trả")]
    public Guid? WarehouseId { get; set; }

    [ValidateNever]
    public EntityOptionListModel? AvailableWarehouses { get; set; }

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

    public Guid? DeliveryNoteItemId { get; set; }

    [Display(Name = "Số lượng yêu cầu")]
    public decimal RequestedQuantity { get; set; }

    [Display(Name = "Số lượng chấp nhận")]
    public decimal AcceptedQuantity { get; set; }

    [Display(Name = "Đơn giá")]
    public decimal UnitPrice { get; set; }
}
