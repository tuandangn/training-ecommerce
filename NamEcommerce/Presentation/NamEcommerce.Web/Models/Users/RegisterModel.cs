using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Users;

[Serializable]
public sealed class RegisterModel
{
    [Display(Name = "Email")]
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string? Fullname { get; set; }

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Display(Name = "Xác nhận mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu và xác nhận không khớp")]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Lưu lại")]
    public bool Persistent { get; set; }
}
