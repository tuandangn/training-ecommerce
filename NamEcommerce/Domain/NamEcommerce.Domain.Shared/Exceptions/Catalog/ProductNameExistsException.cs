namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class ProductNameExistsException(string name)  : NamEcommerceDomainException("Error.ProductNameExists", name);



