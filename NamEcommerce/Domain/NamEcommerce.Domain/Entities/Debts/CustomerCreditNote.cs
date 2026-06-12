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

    internal void Cancel()
    {
        if (_allocations.Any(a => !a.IsReversed))
            throw new NamEcommerceDomainException("Error.CreditNote.CannotCancelAllocated");

        Status = CreditNoteStatus.Cancelled;
        CancelledOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
