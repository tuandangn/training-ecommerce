namespace NamEcommerce.Web.Contracts.Models.Users;

[Serializable]
public sealed record AdminUserListModel
{
    public IList<AdminUserListItemModel> Users { get; init; } = [];
}

[Serializable]
public sealed record AdminUserListItemModel
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public required bool IsLocked { get; init; }
    public IList<string> RoleNames { get; init; } = [];
}

[Serializable]
public sealed record CreateAdminUserModel
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
}

[Serializable]
public sealed record ChangeUserPasswordModel
{
    public required Guid UserId { get; init; }
    public required string Username { get; init; }
    public string? NewPassword { get; set; }
}
