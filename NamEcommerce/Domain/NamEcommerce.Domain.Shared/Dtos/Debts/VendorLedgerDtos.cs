using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

[Serializable]
public sealed record VendorLedgerEntryDto
{
    public required Guid Id { get; init; }
    public required Guid VendorId { get; init; }
    public VendorLedgerEntryType EntryType { get; init; }
    public decimal Amount { get; init; }
    public VendorLedgerReferenceType ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public string? Note { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record VendorLedgerStatementEntryDto
{
    public required Guid EntryId { get; init; }
    public VendorLedgerEntryType EntryType { get; init; }
    public decimal Amount { get; init; }
    public decimal RunningBalance { get; init; }
    public VendorLedgerReferenceType ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public string? Note { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}

[Serializable]
public sealed record VendorAccountBalanceDto
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public string? VendorPhone { get; init; }
    public decimal Balance { get; init; }
    public DateTime? LastEntryOnUtc { get; init; }
}

[Serializable]
public sealed record RecordVendorLedgerChargeDto
{
    public required Guid VendorId { get; init; }
    public required decimal Amount { get; init; }
    public VendorLedgerReferenceType ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.VendorRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.AmountMustBePositive");
    }
}

[Serializable]
public sealed record RecordVendorLedgerPaymentDto
{
    public required Guid VendorId { get; init; }
    public required decimal Amount { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.VendorRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.AmountMustBePositive");
    }
}

[Serializable]
public sealed record RecordVendorLedgerReturnCreditDto
{
    public required Guid VendorId { get; init; }
    public required decimal Amount { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.VendorRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.AmountMustBePositive");
    }
}

[Serializable]
public sealed record RecordVendorLedgerRefundReceiptDto
{
    public required Guid VendorId { get; init; }
    public required decimal Amount { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.VendorRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.AmountMustBePositive");
    }
}

[Serializable]
public sealed record RecordVendorCorrectionDto
{
    public required Guid VendorId { get; init; }
    public required decimal Amount { get; init; }
    public required string Note { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.VendorRequired");
        if (Amount == 0)
            throw new NamEcommerceDomainException("Error.CorrectionAmountCannotBeZero");
        if (string.IsNullOrWhiteSpace(Note))
            throw new NamEcommerceDomainException("Error.CorrectionNoteRequired");
    }
}
