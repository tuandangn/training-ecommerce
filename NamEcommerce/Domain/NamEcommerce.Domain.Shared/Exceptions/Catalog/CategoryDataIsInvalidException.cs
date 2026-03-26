namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class CategoryDataIsInvalidException(string? message) : Exception(message);
