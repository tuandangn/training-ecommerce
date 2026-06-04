using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

[Serializable]
public sealed record CreateBankTransferPaymentIntentDto
{
    public required decimal Amount { get; init; }
    public Guid? CustomerId { get; init; }
    public string? Note { get; init; }

    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string AccountName { get; init; }
    public required string Template { get; init; }
    public required string TransferContentPrefix { get; init; }

    public void Verify()
    {
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.PaymentAmountMustBePositive");
        if (Amount != decimal.Truncate(Amount))
            throw new NamEcommerceDomainException("Error.BankTransferAmountMustBeWholeNumber");
        if (string.IsNullOrWhiteSpace(BankId))
            throw new NamEcommerceDomainException("Error.BankIdRequired");
        if (string.IsNullOrWhiteSpace(AccountNo))
            throw new NamEcommerceDomainException("Error.BankAccountNoRequired");
        if (string.IsNullOrWhiteSpace(AccountName))
            throw new NamEcommerceDomainException("Error.BankAccountNameRequired");
        if (string.IsNullOrWhiteSpace(Template))
            throw new NamEcommerceDomainException("Error.VietQrTemplateRequired");
        if (string.IsNullOrWhiteSpace(TransferContentPrefix))
            throw new NamEcommerceDomainException("Error.TransferContentPrefixRequired");
    }
}

[Serializable]
public sealed record BankTransferPaymentIntentDto(Guid Id)
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public Guid? CustomerId { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string AccountName { get; init; }
    public required string Template { get; init; }
    public required string QrImageUrl { get; init; }
    public required BankTransferPaymentIntentStatus Status { get; init; }
    public string? Note { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public Guid? CustomerPaymentId { get; init; }
    public BankTransferVerificationSource? VerificationSource { get; init; }
    public string? ProviderTransactionId { get; init; }
    public string? RawPayload { get; init; }
    public DateTime? VerifiedAtUtc { get; init; }
    public Guid? VerifiedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}

[Serializable]
public sealed record ConfirmBankTransferPaymentIntentDto
{
    public required Guid IntentId { get; init; }
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required BankTransferVerificationSource Source { get; init; }
    public string? RawPayload { get; init; }
    public DateTime ConfirmedAtUtc { get; init; } = DateTime.UtcNow;
}
