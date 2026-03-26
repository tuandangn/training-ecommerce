namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class ProductNameExistsException(string name) : Exception($"Product with name '{name}' exists");

