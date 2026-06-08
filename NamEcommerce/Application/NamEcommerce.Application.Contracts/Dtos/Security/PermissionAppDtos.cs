namespace NamEcommerce.Application.Contracts.Dtos.Security;

[Serializable]
public sealed record PermissionAppDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}

[Serializable]
public sealed record RolePermissionsAppDto
{
    public required Guid RoleId { get; init; }
    public required string RoleName { get; init; }
    public IList<PermissionAppDto> AllPermissions { get; init; } = [];
    public ISet<Guid> GrantedPermissionIds { get; init; } = new HashSet<Guid>();
}
