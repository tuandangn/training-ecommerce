using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record BankTransferPaymentIntent : AppAggregateEntity
{
    private BankTransferPaymentIntent() : base(Guid.NewGuid()) { }

    internal BankTransferPaymentIntent(
        string referenceCode,
        decimal amount,
        Guid? customerId,
        string bankId,
        string accountNo,
        string accountName,
        string template,
        string qrImageUrl,
        int intentExpiryMinutes,
        string? note) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNo);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(qrImageUrl);
        if (amount <= 0)
            throw new NamEcommerceDomainException("Error.PaymentAmountMustBePositive");

        if (intentExpiryMinutes <= 0)
            throw new NamEcommerceDomainException("Error.PaymentIntentExpiryInvalid");

        var nowUtc = DateTime.UtcNow;
        CreatedOnUtc = nowUtc;
        ExpiresAtUtc = nowUtc.AddMinutes(intentExpiryMinutes);

        ReferenceCode = referenceCode;
        Amount = amount;
        CustomerId = customerId;
        BankId = bankId;
        AccountNo = accountNo;
        AccountName = accountName;
        Template = template;
        QrImageUrl = qrImageUrl;
        Note = note;
        Status = BankTransferPaymentIntentStatus.Pending;
    }

    public string ReferenceCode { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public Guid? CustomerId { get; private set; }

    public string BankId { get; private set; } = string.Empty;
    public string AccountNo { get; private set; } = string.Empty;
    public string AccountName { get; private set; } = string.Empty;
    public string Template { get; private set; } = string.Empty;
    public string QrImageUrl { get; private set; } = string.Empty;

    public BankTransferPaymentIntentStatus Status { get; private set; }
    public string? Note { get; private set; }

    public Guid? OrderId { get; private set; }
    public Guid? DeliveryNoteId { get; private set; }
    public Guid? CustomerDebtId { get; private set; }
    public Guid? CustomerPaymentId { get; private set; }

    public BankTransferVerificationSource? VerificationSource { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public string? RawPayload { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public Guid? VerifiedByUserId { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? ExpiredAtUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal bool CanBeConsumed => Status is BankTransferPaymentIntentStatus.Confirmed or BankTransferPaymentIntentStatus.ManuallyConfirmed;

    internal void Expire(DateTime nowUtc)
    {
        if (Status != BankTransferPaymentIntentStatus.Pending)
            return;

        Status = BankTransferPaymentIntentStatus.Expired;
        ExpiredAtUtc = nowUtc;
        UpdatedOnUtc = nowUtc;
    }

    internal void ConfirmManually(Guid verifiedByUserId, string? note, DateTime nowUtc)
    {
        if (Status != BankTransferPaymentIntentStatus.Pending)
            throw new NamEcommerceDomainException("Error.PaymentIntentCannotConfirm");

        Status = BankTransferPaymentIntentStatus.ManuallyConfirmed;
        VerificationSource = BankTransferVerificationSource.Manual;
        VerifiedByUserId = verifiedByUserId;
        VerifiedAtUtc = nowUtc;
        Note = string.IsNullOrWhiteSpace(note) ? Note : note;
        UpdatedOnUtc = nowUtc;
    }

    internal void ConfirmFromProvider(
        string providerTransactionId,
        BankTransferVerificationSource source,
        string? rawPayload,
        DateTime nowUtc)
    {
        if (Status != BankTransferPaymentIntentStatus.Pending)
            throw new NamEcommerceDomainException("Error.PaymentIntentCannotConfirm");
        if (source == BankTransferVerificationSource.Manual)
            throw new NamEcommerceDomainException("Error.PaymentIntentProviderSourceInvalid");

        ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionId);

        Status = BankTransferPaymentIntentStatus.Confirmed;
        VerificationSource = source;
        ProviderTransactionId = providerTransactionId;
        RawPayload = rawPayload;
        VerifiedAtUtc = nowUtc;
        UpdatedOnUtc = nowUtc;
    }

    internal void Consume(Guid orderId, Guid? deliveryNoteId, Guid? customerDebtId, Guid customerPaymentId, DateTime nowUtc)
    {
        if (!CanBeConsumed)
            throw new NamEcommerceDomainException("Error.PaymentIntentCannotConsume");
        if (orderId == Guid.Empty || customerPaymentId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.PaymentIntentCannotConsume");

        OrderId = orderId;
        DeliveryNoteId = deliveryNoteId;
        CustomerDebtId = customerDebtId;
        CustomerPaymentId = customerPaymentId;
        Status = BankTransferPaymentIntentStatus.Consumed;
        UpdatedOnUtc = nowUtc;
    }

    internal void Cancel(DateTime nowUtc)
    {
        if (Status == BankTransferPaymentIntentStatus.Consumed)
            throw new NamEcommerceDomainException("Error.PaymentIntentCannotCancel");

        Status = BankTransferPaymentIntentStatus.Cancelled;
        UpdatedOnUtc = nowUtc;
    }
}
