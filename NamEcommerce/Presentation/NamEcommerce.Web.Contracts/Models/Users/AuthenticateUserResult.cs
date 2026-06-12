namespace NamEcommerce.Web.Contracts.Models.Users;

[Serializable]
public sealed record AuthenticateUserResult : ICommandResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public IList<string> RoleNames { get; init; } = [];
}
