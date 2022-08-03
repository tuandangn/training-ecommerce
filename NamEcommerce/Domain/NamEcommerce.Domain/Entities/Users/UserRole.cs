using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Users;

[Serializable]
public sealed record UserRole : AppEntity
{
    public UserRole(Guid id, Guid userId, Guid roleId) : base(id)
        => (UserId, RoleId) = (userId, roleId);

    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
}
