namespace NamEcommerce.Domain.Shared.Exceptions.Users;

[Serializable]
public sealed class UsernameExistsException(string username)  : NamEcommerceDomainException("Error.UsernameExistsException", username);

