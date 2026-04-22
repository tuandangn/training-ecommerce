namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class CategoryIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.CategoryIsNotFound", id);


