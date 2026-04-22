namespace NamEcommerce.Domain.Shared.Exceptions.Debts;

[Serializable]
public sealed class VendorPaymentExceedsRemainingException(decimal paymentAmount, decimal remainingAmount)
     : NamEcommerceDomainException("Error.VendorPaymentExceedsRemainingException", paymentAmount, remainingAmount);

