namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class VendorNameRequiredException() : NamEcommerceDomainException("Error.VendorNameRequired");
