namespace NamEcommerce.Domain.Shared.Exceptions.Users;

[Serializable]
public sealed class UserAlreadyHaveRoleException : Exception
{
    public UserAlreadyHaveRoleException(int roleId, string username)
        : base($"User '{username}' already have role with id '{roleId}'")
    {
    }
}
