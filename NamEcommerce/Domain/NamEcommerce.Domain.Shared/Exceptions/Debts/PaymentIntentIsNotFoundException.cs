namespace NamEcommerce.Domain.Shared.Exceptions.Debts;

[Serializable]
public sealed class PaymentIntentIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.PaymentIntentIsNotFound", id);


