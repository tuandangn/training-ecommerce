using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record VendorCreditNoteAllocation : AppEntity
{
    private VendorCreditNoteAllocation() : base(Guid.Empty) { }

    internal VendorCreditNoteAllocation(
        Guid id,
        Guid vendorCreditNoteId,
        string vendorCreditNoteCode,
        Guid sourceReturnId,
        string sourceReturnCode,
        Guid vendorDebtId,
        string vendorDebtCode,
        decimal amount,
        Guid? appliedByUserId) : base(id)
    {
        VendorCreditNoteId = vendorCreditNoteId;
        VendorCreditNoteCode = vendorCreditNoteCode;
        SourceReturnId = sourceReturnId;
        SourceReturnCode = sourceReturnCode;
        VendorDebtId = vendorDebtId;
        VendorDebtCode = vendorDebtCode;
        Amount = amount;
        AppliedOnUtc = DateTime.UtcNow;
        AppliedByUserId = appliedByUserId;
    }

    public Guid VendorCreditNoteId { get; private set; }
    public string VendorCreditNoteCode { get; private set; } = string.Empty;
    public Guid SourceReturnId { get; private set; }
    public string SourceReturnCode { get; private set; } = string.Empty;
    public Guid VendorDebtId { get; private set; }
    public string VendorDebtCode { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime AppliedOnUtc { get; private set; }
    public Guid? AppliedByUserId { get; private set; }
    public DateTime? ReversedOnUtc { get; private set; }
    public Guid? ReversedByUserId { get; private set; }
    public string? ReverseReason { get; private set; }
    public bool IsReversed => ReversedOnUtc.HasValue;

    internal void MarkReversed(Guid? reversedByUserId, string? reverseReason)
    {
        if (IsReversed) return;

        ReversedByUserId = reversedByUserId;
        ReverseReason = reverseReason;
        ReversedOnUtc = DateTime.UtcNow;
    }
}
