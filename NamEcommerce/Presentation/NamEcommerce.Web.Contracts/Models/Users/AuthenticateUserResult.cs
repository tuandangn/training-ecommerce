namespace NamEcommerce.Web.Contracts.Models.Users;

[Serializable]
public sealed record AuthenticateUserResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }
}
