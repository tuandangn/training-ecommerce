namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class ProductAlreadyInCategoryException : Exception
{
    public ProductAlreadyInCategoryException(Guid categoryId, string productName)
        : base($"Product '{productName}' already have been in category with id '{categoryId}'")
    {
    }
}
