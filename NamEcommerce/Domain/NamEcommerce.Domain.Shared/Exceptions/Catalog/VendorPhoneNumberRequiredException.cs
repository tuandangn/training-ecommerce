namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class VendorPhoneNumberRequiredException() : NamEcommerceDomainException("Error.VendorPhoneNumberRequired");
