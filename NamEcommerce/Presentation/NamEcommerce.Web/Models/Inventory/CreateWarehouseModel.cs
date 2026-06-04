using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Inventory;

[Serializable]
public sealed class CreateWarehouseModel
{
    [Display(Name = "Mã kho")]
    public string? Code { get; set; }
    [ValidateNever]
    public string CodePrefix { get; set; } = "";

    [Display(Name = "Tên kho")]
    public string? Name { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }
}
