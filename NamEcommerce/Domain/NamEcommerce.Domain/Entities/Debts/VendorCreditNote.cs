using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record VendorCreditNote : AppAggregateEntity
{
    private readonly IList<VendorCreditNoteAllocation> _allocations = [];

    private VendorCreditNote() : base(Guid.Empty) { }

    internal VendorCreditNote(
        string code,
        Guid vendorId,
        string vendorName,
        Guid sourceReturnId,
        string sourceReturnCode,
        Guid? sourceGoodsReceiptId,
        Guid? sourcePurchaseOrderId,
        decimal amount) : base(Guid.NewGuid())
    {
        if (amount <= 0)
            throw new NamEcommerceDomainException("Error.CreditNote.AmountMustBePositive");

        Code = code;
        VendorId = vendorId;
        VendorName = vendorName;
        SourceType = CreditNoteSourceType.VendorReturn;
        SourceReturnId = sourceReturnId;
        SourceReturnCode = sourceReturnCode;
        SourceGoodsReceiptId = sourceGoodsReceiptId;
        SourcePurchaseOrderId = sourcePurchaseOrderId;
        Amount = amount;
        RemainingAmount = amount;
        Status = CreditNoteStatus.Unapplied;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;
    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; } = string.Empty;
    public CreditNoteSourceType SourceType { get; private set; }
    public Guid SourceReturnId { get; private set; }
    public string SourceReturnCode { get; private set; } = string.Empty;
    public Guid? SourceGoodsReceiptId { get; private set; }
    public Guid? SourcePurchaseOrderId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal AppliedAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public CreditNoteStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public DateTime? CancelledOnUtc { get; private set; }
    public IReadOnlyCollection<VendorCreditNoteAllocation> Allocations => _allocations.AsReadOnly();

    internal void Cancel()
    {
        if (_allocations.Any(a => !a.IsReversed))
            throw new NamEcommerceDomainException("Error.CreditNote.CannotCancelAllocated");

        Status = CreditNoteStatus.Cancelled;
        CancelledOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
