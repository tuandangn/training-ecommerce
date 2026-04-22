namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class UserDataIsInvalidException(string? message)  : NamEcommerceDomainException("Error.UserDataIsInvalidException", message);

