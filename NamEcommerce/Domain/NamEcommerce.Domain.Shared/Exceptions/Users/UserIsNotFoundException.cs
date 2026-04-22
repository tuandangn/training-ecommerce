namespace NamEcommerce.Domain.Shared.Exceptions.Users;

[Serializable]
public sealed class UserIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.UserIsNotFound", id);


