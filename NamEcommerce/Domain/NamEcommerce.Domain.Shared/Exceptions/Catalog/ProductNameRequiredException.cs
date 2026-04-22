namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class ProductNameRequiredException() : NamEcommerceDomainException("Error.ProductNameRequired");
