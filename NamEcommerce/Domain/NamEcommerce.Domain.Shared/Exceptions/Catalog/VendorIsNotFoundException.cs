namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class VendorIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.VendorIsNotFound", id);


