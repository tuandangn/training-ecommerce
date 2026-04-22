namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class VendorNameExistsException(string name)  : NamEcommerceDomainException("Error.VendorNameExists", name);


