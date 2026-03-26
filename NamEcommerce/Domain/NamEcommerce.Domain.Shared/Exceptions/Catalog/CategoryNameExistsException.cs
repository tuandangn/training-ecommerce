namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class CategoryNameExistsException(string name) : Exception($"Category with name '{name}' exists");

