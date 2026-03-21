namespace NamEcommerce.Web.Models.Common;

[Serializable]
public sealed record UserHeaderLinksModel
{
    public required bool IsAuthenticated { get; init; }

    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
}
