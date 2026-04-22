namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class ProductIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.ProductIsNotFoundException", id);

