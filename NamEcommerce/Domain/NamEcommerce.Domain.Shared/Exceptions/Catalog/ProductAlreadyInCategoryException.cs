namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class ProductAlreadyInCategoryException(Guid categoryId, string productName)
    : NamEcommerceDomainException("Error.ProductAlreadyInCategory", productName, categoryId);
