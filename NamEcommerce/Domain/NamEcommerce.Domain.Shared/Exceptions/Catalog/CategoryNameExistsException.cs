namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class CategoryNameExistsException(string name)  : NamEcommerceDomainException("Error.CategoryNameExistsException", name);


