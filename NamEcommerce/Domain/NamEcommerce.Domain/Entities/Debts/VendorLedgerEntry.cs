using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Events.Debts;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record VendorLedgerEntry : AppAggregateEntity
{
    public VendorLedgerEntry(Guid id) : base(id)
    {
    }

    internal VendorLedgerEntry(
        Guid vendorId,
        VendorLedgerEntryType entryType,
        decimal amount,
        VendorLedgerReferenceType referenceType,
        Guid? referenceId,
        string? referenceCode,
        string? note,
        DateTime occurredAtUtc,
        Guid? createdByUserId) : base(Guid.NewGuid())
    {
        VendorId = vendorId;
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

    public Guid VendorId { get; private set; }
    public VendorLedgerEntryType EntryType { get; private set; }
    public decimal Amount { get; private set; }
    public VendorLedgerReferenceType ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? ReferenceCode { get; private set; }
    public string? Note { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    internal void MarkRecorded()
        => RaiseDomainEvent(new VendorLedgerEntryRecorded(Id, VendorId, EntryType, Amount, ReferenceId));
}
