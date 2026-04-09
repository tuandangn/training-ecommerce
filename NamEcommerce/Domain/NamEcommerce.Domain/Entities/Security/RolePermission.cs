using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Security;

[Serializable]
public sealed record RolePermission : AppEntity
{
    public RolePermission(Guid roleId, Guid permissionId) : base(Guid.Empty)
        => (RoleId, PermissionId) = (roleId, permissionId);

    public Guid RoleId { get; init; }
    public Guid PermissionId { get; init; }
}
