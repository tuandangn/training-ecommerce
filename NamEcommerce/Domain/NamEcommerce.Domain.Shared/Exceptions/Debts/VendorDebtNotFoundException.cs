namespace NamEcommerce.Domain.Shared.Exceptions.Debts;

[Serializable]
public sealed class VendorDebtNotFoundException(Guid id)  : NamEcommerceDomainException("Error.VendorDebtNotFound", id);


