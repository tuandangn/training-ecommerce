namespace NamEcommerce.Domain.Shared.Exceptions.Finance;

[Serializable]
public sealed class ExpenseDataIsInvalidException(string errorCode) : NamEcommerceDomainException(errorCode);
