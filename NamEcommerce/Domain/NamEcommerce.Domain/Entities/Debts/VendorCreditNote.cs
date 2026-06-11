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

    internal VendorCreditNoteAllocation AllocateToDebt(VendorDebt debt, decimal amount, Guid? appliedByUserId)
    {
        if (Status == CreditNoteStatus.Cancelled)
            throw new NamEcommerceDomainException("Error.CreditNote.Cancelled");
        if (amount <= 0)
            throw new NamEcommerceDomainException("Error.CreditNote.AllocationAmountMustBePositive");
        if (amount > RemainingAmount)
            throw new NamEcommerceDomainException("Error.CreditNote.AllocationAmountExceedsRemaining", amount, RemainingAmount);

        debt.ApplyCreditNote(amount);
        var allocation = new VendorCreditNoteAllocation(
            Guid.NewGuid(),
            Id,
            Code,
            SourceReturnId,
            SourceReturnCode,
            debt.Id,
            debt.Code,
            amount,
            appliedByUserId);
        _allocations.Add(allocation);

        AppliedAmount += amount;
        RemainingAmount = Amount - AppliedAmount;
        Status = RemainingAmount <= 0 ? CreditNoteStatus.FullyApplied : CreditNoteStatus.PartiallyApplied;
        UpdatedOnUtc = DateTime.UtcNow;

        return allocation;
    }

    internal void ReverseAllocation(VendorCreditNoteAllocation allocation, Guid? reversedByUserId, string reason)
    {
        if (allocation.IsReversed) return;

        allocation.MarkReversed(reversedByUserId, reason);
        AppliedAmount -= allocation.Amount;
        if (AppliedAmount < 0)
            AppliedAmount = 0;

        RemainingAmount = Amount - AppliedAmount;
        Status = AppliedAmount <= 0
            ? CreditNoteStatus.Unapplied
            : CreditNoteStatus.PartiallyApplied;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Đánh dấu phần còn lại của credit note đã được giải quyết bằng thu tiền mặt từ NCC.
    /// Gọi khi VendorRefund hoàn thành để credit note không còn treo trên BalanceSheet.
    /// </summary>
    internal void ConsumeByRefund(decimal refundAmount)
    {
        var consume = Math.Min(refundAmount, RemainingAmount);
        if (consume <= 0) return;

        AppliedAmount += consume;
        RemainingAmount = Amount - AppliedAmount;
        Status = RemainingAmount <= 0 ? CreditNoteStatus.FullyApplied : CreditNoteStatus.PartiallyApplied;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Cancel()
    {
        if (_allocations.Any(a => !a.IsReversed))
            throw new NamEcommerceDomainException("Error.CreditNote.CannotCancelAllocated");

        Status = CreditNoteStatus.Cancelled;
        CancelledOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
