using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record BankTransferVerificationLog : AppAggregateEntity
{
    private BankTransferVerificationLog() : base(Guid.NewGuid()) { }

    internal BankTransferVerificationLog(
        string referenceCode,
        decimal amount,
        string bankId,
        string accountNo,
        string providerTransactionId,
        BankTransferVerificationSource source,
        string? rawPayload,
        DateTime providerConfirmedAtUtc) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNo);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionId);
        if (amount <= 0)
            throw new NamEcommerceDomainException("Error.PaymentAmountMustBePositive");
        if (!Enum.IsDefined(source))
            throw new NamEcommerceDomainException("Error.BankTransferVerificationSourceInvalid");

        ReferenceCode = referenceCode;
        Amount = amount;
        BankId = bankId;
        AccountNo = accountNo;
        ProviderTransactionId = providerTransactionId;
        Source = source;
        Status = BankTransferVerificationLogStatus.Received;
        RawPayload = rawPayload;
        ProviderConfirmedAtUtc = providerConfirmedAtUtc;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string ReferenceCode { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string BankId { get; private set; } = string.Empty;
    public string AccountNo { get; private set; } = string.Empty;
    public string ProviderTransactionId { get; private set; } = string.Empty;
    public BankTransferVerificationSource Source { get; private set; }
    public BankTransferVerificationLogStatus Status { get; private set; }
    public Guid? PaymentIntentId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? RawPayload { get; private set; }
    public DateTime ProviderConfirmedAtUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal void MarkMatched(Guid paymentIntentId, DateTime nowUtc)
    {
        PaymentIntentId = paymentIntentId;
        Status = BankTransferVerificationLogStatus.Matched;
        UpdatedOnUtc = nowUtc;
    }

    internal void MarkRejected(string errorMessage, DateTime nowUtc)
    {
        ErrorMessage = errorMessage;
        Status = BankTransferVerificationLogStatus.Rejected;
        UpdatedOnUtc = nowUtc;
    }

    internal void MarkDuplicate(Guid? paymentIntentId, string errorMessage, DateTime nowUtc)
    {
        PaymentIntentId = paymentIntentId;
        ErrorMessage = errorMessage;
        Status = BankTransferVerificationLogStatus.Duplicate;
        UpdatedOnUtc = nowUtc;
    }
}
