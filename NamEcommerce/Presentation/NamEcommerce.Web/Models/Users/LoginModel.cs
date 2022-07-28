namespace NamEcommerce.Web.Models.Users;

[Serializable]
public sealed class LoginModel
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public bool Persistent { get; set; }
}
