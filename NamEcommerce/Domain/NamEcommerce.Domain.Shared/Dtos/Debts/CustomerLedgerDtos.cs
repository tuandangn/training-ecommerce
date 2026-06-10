using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

[Serializable]
public sealed record CustomerLedgerEntryDto
{
    public required Guid Id { get; init; }
    public required Guid CustomerId { get; init; }
    public CustomerLedgerEntryType EntryType { get; init; }
    public decimal Amount { get; init; }
    public CustomerLedgerReferenceType ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public string? Note { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CustomerLedgerStatementEntryDto
{
    public required Guid EntryId { get; init; }
    public CustomerLedgerEntryType EntryType { get; init; }
    public decimal Amount { get; init; }
    public decimal RunningBalance { get; init; }
    public CustomerLedgerReferenceType ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public string? Note { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}

[Serializable]
public sealed record CustomerAccountBalanceDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public decimal Balance { get; init; }
    public DateTime? LastEntryOnUtc { get; init; }
}

[Serializable]
public sealed record RecordCustomerLedgerChargeDto
{
    public required Guid CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public CustomerLedgerReferenceType ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.AmountMustBePositive");
    }
}

[Serializable]
public sealed record RecordCustomerLedgerPaymentDto
{
    public required Guid CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.AmountMustBePositive");
    }
}

[Serializable]
public sealed record RecordCustomerLedgerReturnCreditDto
{
    public required Guid CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.AmountMustBePositive");
    }
}

[Serializable]
public sealed record RecordCustomerLedgerRefundPayoutDto
{
    public required Guid CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.AmountMustBePositive");
    }
}

[Serializable]
public sealed record RecordCustomerCorrectionDto
{
    public required Guid CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required string Note { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerRequired");
        if (Amount == 0)
            throw new NamEcommerceDomainException("Error.CorrectionAmountCannotBeZero");
        if (string.IsNullOrWhiteSpace(Note))
            throw new NamEcommerceDomainException("Error.CorrectionNoteRequired");
    }
}
