using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Security;

[Serializable]
public sealed record RolePermission : AppEntity
{
    public RolePermission(int id, int roleId, int permissionId) : base(id)
        => (RoleId, PermissionId) = (roleId, permissionId);

    public int RoleId { get; init; }
    public int PermissionId { get; init; }
}
