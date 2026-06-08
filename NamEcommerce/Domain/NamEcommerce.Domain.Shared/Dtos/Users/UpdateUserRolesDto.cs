using NamEcommerce.Domain.Shared.Exceptions.Users;

namespace NamEcommerce.Domain.Shared.Dtos.Users;

[Serializable]
public sealed record UpdateUserRolesDto(Guid UserId, IList<Guid> RoleIds)
{
    public void Verify()
    {
        if (UserId == Guid.Empty)
            throw new UserDataIsInvalidException("Error.UserRequired");

        if (RoleIds is null)
            throw new UserDataIsInvalidException("Error.RoleIdsRequired");

        if (RoleIds.Any(roleId => roleId == Guid.Empty))
            throw new UserDataIsInvalidException("Error.RoleIsInvalid");

        if (RoleIds.Distinct().Count() != RoleIds.Count)
            throw new UserDataIsInvalidException("Error.RoleDuplicated");
    }
}
