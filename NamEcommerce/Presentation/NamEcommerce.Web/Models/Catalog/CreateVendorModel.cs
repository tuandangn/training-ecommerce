using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed class CreateVendorModel
{
    [Display(Name = "Tên nhà cung cấp")]
    public string? Name { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }
}
