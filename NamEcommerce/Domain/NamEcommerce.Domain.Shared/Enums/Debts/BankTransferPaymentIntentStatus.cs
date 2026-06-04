namespace NamEcommerce.Domain.Shared.Enums.Debts;

public enum BankTransferPaymentIntentStatus
{
    Pending = 10,
    Confirmed = 20,
    ManuallyConfirmed = 30,
    Expired = 40,
    Cancelled = 50,
    Consumed = 60
}

public enum BankTransferVerificationSource
{
    Manual = 10,
    BankWebhook = 20,
    BankStatement = 30
}
