namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class CategoryNameRequiredException() : NamEcommerceDomainException("Error.CategoryNameRequired");
