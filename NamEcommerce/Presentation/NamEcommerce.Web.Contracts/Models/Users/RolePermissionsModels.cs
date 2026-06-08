namespace NamEcommerce.Web.Contracts.Models.Users;

[Serializable]
public sealed record RolePermissionsPageModel
{
    public required Guid SelectedRoleId { get; init; }
    public IList<RoleSelectionModel> AvailableRoles { get; init; } = [];
    public required RolePermissionsModel CurrentRole { get; init; }
}

[Serializable]
public sealed record RolePermissionsModel
{
    public required Guid RoleId { get; init; }
    public required string RoleName { get; init; }
    public IList<PermissionGroupModel> Groups { get; init; } = [];
}

[Serializable]
public sealed record PermissionGroupModel
{
    public required string GroupName { get; init; }
    public IList<PermissionItemModel> Items { get; init; } = [];
}

[Serializable]
public sealed record PermissionItemModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Label { get; init; }
    public required bool IsGranted { get; init; }
}
