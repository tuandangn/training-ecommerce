namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class CategoryCircularRelationshipException(string category1, string category2)
    : NamEcommerceDomainException("Error.CategoryCircularRelationship", category1, category2);
