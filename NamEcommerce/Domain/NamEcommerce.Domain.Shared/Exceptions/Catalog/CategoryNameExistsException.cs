namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class CategoryNameExistsException : Exception
{
    public CategoryNameExistsException(string name) : base($"Category with name '{name}' exists")
    {
    }
}
