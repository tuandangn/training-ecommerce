using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Users;

[Serializable]
public sealed class LoginModel
{
    [Display(Name = "Tên đăng nhập")]
    public string? Username { get; set; }

    [Display(Name = "Mật khẩu")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
