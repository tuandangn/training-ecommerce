namespace NamEcommerce.Domain.Shared.Exceptions.Users;

[Serializable]
public sealed class UsernameExistsException : Exception
{
    public UsernameExistsException(string username) : base($"User with username '{username}' exists")
    {
    }
}
