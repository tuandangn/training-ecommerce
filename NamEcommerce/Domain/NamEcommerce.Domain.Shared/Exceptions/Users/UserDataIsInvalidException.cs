namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class UserDataIsInvalidException(string? message) : Exception(message);
