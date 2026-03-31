namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class VendorIsNotFoundException(Guid id) : Exception($"Vendor with id '{id}' is not found");
