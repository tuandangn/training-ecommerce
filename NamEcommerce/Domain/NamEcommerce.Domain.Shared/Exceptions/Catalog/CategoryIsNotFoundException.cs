namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class CategoryIsNotFoundException(Guid id) : Exception($"Category with id '{id}' is not found");
