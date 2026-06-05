using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

[Serializable]
public sealed record CreateBankTransferVerificationLogDto
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required BankTransferVerificationSource Source { get; init; }
    public string? RawPayload { get; init; }
    public DateTime ProviderConfirmedAtUtc { get; init; } = DateTime.UtcNow;

    public void Verify()
    {
        if (string.IsNullOrWhiteSpace(ReferenceCode))
            throw new NamEcommerceDomainException("Error.ReferenceCodeRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.PaymentAmountMustBePositive");
        if (string.IsNullOrWhiteSpace(BankId))
            throw new NamEcommerceDomainException("Error.BankIdRequired");
        if (string.IsNullOrWhiteSpace(AccountNo))
            throw new NamEcommerceDomainException("Error.BankAccountNoRequired");
        if (string.IsNullOrWhiteSpace(ProviderTransactionId))
            throw new NamEcommerceDomainException("Error.ProviderTransactionIdRequired");
        if (!Enum.IsDefined(Source))
            throw new NamEcommerceDomainException("Error.BankTransferVerificationSourceInvalid");
    }
}

[Serializable]
public sealed record BankTransferVerificationLogDto(Guid Id)
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required BankTransferVerificationSource Source { get; init; }
    public required BankTransferVerificationLogStatus Status { get; init; }
    public Guid? PaymentIntentId { get; init; }
    public string? ErrorMessage { get; init; }
    public string? RawPayload { get; init; }
    public DateTime ProviderConfirmedAtUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}
