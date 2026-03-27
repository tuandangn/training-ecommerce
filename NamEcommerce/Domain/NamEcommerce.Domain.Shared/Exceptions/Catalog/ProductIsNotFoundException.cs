namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class ProductIsNotFoundException(Guid id) : Exception($"Product with id '{id}' is not found");
