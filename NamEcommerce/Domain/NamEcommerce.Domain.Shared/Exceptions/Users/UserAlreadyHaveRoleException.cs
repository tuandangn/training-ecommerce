namespace NamEcommerce.Domain.Shared.Exceptions.Users;

[Serializable]
public sealed class UserAlreadyHaveRoleException : NamEcommerceDomainException
{
    public UserAlreadyHaveRoleException(Guid roleId, string username)
        : base("Error.UserAlreadyHaveRole", roleId, username)
    {
    }
}
