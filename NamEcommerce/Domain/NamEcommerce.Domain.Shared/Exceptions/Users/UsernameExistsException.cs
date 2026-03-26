namespace NamEcommerce.Domain.Shared.Exceptions.Users;

[Serializable]
public sealed class UsernameExistsException(string username) : Exception($"User with username '{username}' exists");
