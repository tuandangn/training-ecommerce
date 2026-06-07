namespace NamEcommerce.Web.Contracts.Models.Users;

[Serializable]
public sealed record UserRoleListModel
{
    public IList<UserRoleListItemModel> Users { get; init; } = [];
}

[Serializable]
public sealed record UserRoleListItemModel
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public IList<string> RoleNames { get; init; } = [];
}

[Serializable]
public sealed record UserRoleAssignmentModel
{
    public required Guid UserId { get; init; }
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public IList<Guid> RoleIds { get; init; } = [];
    public IList<RoleSelectionModel> AvailableRoles { get; init; } = [];
}

[Serializable]
public sealed record RoleSelectionModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool Selected { get; init; }
}
