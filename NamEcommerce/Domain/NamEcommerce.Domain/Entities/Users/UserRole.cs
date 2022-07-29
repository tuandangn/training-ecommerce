using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Users;

[Serializable]
public sealed record UserRole : AppEntity
{
    public UserRole(int id, int userId, int roleId) : base(id)
        => (UserId, RoleId) = (userId, roleId);

    public int UserId { get; init; }
    public int RoleId { get; init; }
}
