namespace NamEcommerce.Domain.Shared.Exceptions.Users;

[Serializable]
public sealed class UserDataIsInvalidException(string errorCode, params object[] parameters) : NamEcommerceDomainException(errorCode, parameters);


