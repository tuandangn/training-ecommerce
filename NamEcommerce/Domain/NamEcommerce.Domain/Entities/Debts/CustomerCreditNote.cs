using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record CustomerCreditNote : AppAggregateEntity
{
    private readonly IList<CustomerCreditNoteAllocation> _allocations = [];

    private CustomerCreditNote() : base(Guid.Empty) { }

    internal CustomerCreditNote(
        string code,
        Guid customerId,
        string customerName,
        Guid sourceReturnId,
        string sourceReturnCode,
        Guid? sourceDeliveryNoteId,
        decimal amount) : base(Guid.NewGuid())
    {
        if (amount <= 0)
            throw new NamEcommerceDomainException("Error.CreditNote.AmountMustBePositive");

        Code = code;
        CustomerId = customerId;
        CustomerName = customerName;
        SourceType = CreditNoteSourceType.CustomerReturn;
        SourceReturnId = sourceReturnId;
        SourceReturnCode = sourceReturnCode;
        SourceDeliveryNoteId = sourceDeliveryNoteId;
        Amount = amount;
        RemainingAmount = amount;
        Status = CreditNoteStatus.Unapplied;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public CreditNoteSourceType SourceType { get; private set; }
    public Guid SourceReturnId { get; private set; }
    public string SourceReturnCode { get; private set; } = string.Empty;
    public Guid? SourceDeliveryNoteId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal AppliedAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public CreditNoteStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public DateTime? CancelledOnUtc { get; private set; }
    public IReadOnlyCollection<CustomerCreditNoteAllocation> Allocations => _allocations.AsReadOnly();

    internal CustomerCreditNoteAllocation AllocateToDebt(CustomerDebt debt, decimal amount, Guid? appliedByUserId)
    {
        if (Status == CreditNoteStatus.Cancelled)
            throw new NamEcommerceDomainException("Error.CreditNote.Cancelled");
        if (amount <= 0)
            throw new NamEcommerceDomainException("Error.CreditNote.AllocationAmountMustBePositive");
        if (amount > RemainingAmount)
            throw new NamEcommerceDomainException("Error.CreditNote.AllocationAmountExceedsRemaining", amount, RemainingAmount);

        debt.ApplyCreditNote(amount);
        var allocation = new CustomerCreditNoteAllocation(
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

    /// <summary>
    /// Đánh dấu phần còn lại của credit note đã được giải quyết bằng hoàn tiền mặt.
    /// Gọi khi CustomerRefund hoàn thành để credit note không còn treo trên BalanceSheet.
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
