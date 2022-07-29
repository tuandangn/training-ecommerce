namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class ProductAlreadyInCategoryException : Exception
{
    public ProductAlreadyInCategoryException(int categoryId, string productName)
        : base($"Product '{productName}' already have been in category with id '{categoryId}'")
    {
    }
}
