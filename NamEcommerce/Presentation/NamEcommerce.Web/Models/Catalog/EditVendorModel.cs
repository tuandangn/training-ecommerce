using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed record EditVendorModel
{
    public Guid Id { get; set; }

    [Display(Name = "Tên nhà cung cấp")]
    public required string Name { get; set; }

    [Display(Name = "Số điện thoại")]
    public required string PhoneNumber { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }
}
