namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class CategoryCircularRelationshipException : Exception
{
    public CategoryCircularRelationshipException(string category1, string category2)
        : base($"Category '{category1}' and category '{category2}' are circular relationship")
    {
    }
}
