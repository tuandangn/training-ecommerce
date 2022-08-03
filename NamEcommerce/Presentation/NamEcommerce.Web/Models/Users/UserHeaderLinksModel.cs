namespace NamEcommerce.Web.Models.Users;

[Serializable]
public sealed class UserHeaderLinksModel
{
    public bool IsAuthenticated { get; set; }

    public string Username { get; set; } = string.Empty;
}
