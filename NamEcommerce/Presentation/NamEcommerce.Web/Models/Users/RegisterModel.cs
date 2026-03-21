using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Users;

[Serializable]
public sealed class RegisterModel
{
    [Display(Name = "Tên đăng nhập")]
    public string? Username { get; set; }

    [Display(Name = "Mật khẩu")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Display(Name = "Xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Họ tên")]
    public string? Fullname { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }
}
