namespace NamEcommerce.Domain.Shared.Exceptions.Users;

[Serializable]
public sealed class UserIsNotFoundException(Guid id) : Exception($"User with id '{id}' is not found");
