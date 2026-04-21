namespace NamEcommerce.Domain.Shared.Exceptions.Debts;

[Serializable]
public sealed class VendorDebtNotFoundException(Guid id) : Exception($"VendorDebt with id '{id}' is not found");
