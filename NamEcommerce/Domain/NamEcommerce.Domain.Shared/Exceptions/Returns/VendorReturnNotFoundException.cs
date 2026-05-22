namespace NamEcommerce.Domain.Shared.Exceptions.Returns;

[Serializable]
public sealed class VendorReturnNotFoundException(Guid id)
    : NamEcommerceDomainException("Error.VendorReturn.NotFound", id);
