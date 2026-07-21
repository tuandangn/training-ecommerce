using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Users;

[Serializable]
public sealed record UserRole : AppEntity
{
    internal UserRole(Guid userId, Guid roleId) : base(Guid.Empty)
        => (UserId, RoleId) = (userId, roleId);

    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
}
