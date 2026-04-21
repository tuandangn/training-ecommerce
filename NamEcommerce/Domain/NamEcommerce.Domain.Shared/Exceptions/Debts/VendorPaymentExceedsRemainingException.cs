namespace NamEcommerce.Domain.Shared.Exceptions.Debts;

[Serializable]
public sealed class VendorPaymentExceedsRemainingException(decimal paymentAmount, decimal remainingAmount)
    : Exception($"Payment amount '{paymentAmount}' exceeds remaining debt amount '{remainingAmount}'");
