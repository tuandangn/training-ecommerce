namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class VendorDataIsInvalidException(string? message)  : NamEcommerceDomainException("Error.VendorDataIsInvalidException", message);

