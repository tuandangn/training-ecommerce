using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Events.Debts;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record CustomerLedgerEntry : AppAggregateEntity
{
    public CustomerLedgerEntry(Guid id) : base(id)
    {
    }

    internal CustomerLedgerEntry(
        Guid customerId,
        CustomerLedgerEntryType entryType,
        decimal amount,
        CustomerLedgerReferenceType referenceType,
        Guid? referenceId,
        string? referenceCode,
        string? note,
        DateTime occurredAtUtc,
        Guid? createdByUserId) : base(Guid.NewGuid())
    {
        CustomerId = customerId;
        EntryType = entryType;
        Amount = amount;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        ReferenceCode = referenceCode;
        Note = note;
        OccurredAtUtc = occurredAtUtc;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public CustomerLedgerEntryType EntryType { get; private set; }
    public decimal Amount { get; private set; }
    public CustomerLedgerReferenceType ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? ReferenceCode { get; private set; }
    public string? Note { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    internal void MarkRecorded()
        => RaiseDomainEvent(new CustomerLedgerEntryRecorded(Id, CustomerId, EntryType, Amount, ReferenceId));
}
