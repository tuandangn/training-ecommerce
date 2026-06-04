using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record CustomerCreditNoteAllocation : AppEntity
{
    private CustomerCreditNoteAllocation() : base(Guid.Empty) { }

    internal CustomerCreditNoteAllocation(
        Guid id,
        Guid customerCreditNoteId,
        string customerCreditNoteCode,
        Guid sourceReturnId,
        string sourceReturnCode,
        Guid customerDebtId,
        string customerDebtCode,
        decimal amount,
        Guid? appliedByUserId) : base(id)
    {
        CustomerCreditNoteId = customerCreditNoteId;
        CustomerCreditNoteCode = customerCreditNoteCode;
        SourceReturnId = sourceReturnId;
        SourceReturnCode = sourceReturnCode;
        CustomerDebtId = customerDebtId;
        CustomerDebtCode = customerDebtCode;
        Amount = amount;
        AppliedOnUtc = DateTime.UtcNow;
        AppliedByUserId = appliedByUserId;
    }

    public Guid CustomerCreditNoteId { get; private set; }
    public string CustomerCreditNoteCode { get; private set; } = string.Empty;
    public Guid SourceReturnId { get; private set; }
    public string SourceReturnCode { get; private set; } = string.Empty;
    public Guid CustomerDebtId { get; private set; }
    public string CustomerDebtCode { get; private set; } = string.Empty;
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
