using NamEcommerce.Domain.Shared.Enums.Debts;

namespace NamEcommerce.Domain.Shared.Events.Debts;

public sealed record VendorLedgerEntryRecorded(
    Guid EntryId,
    Guid VendorId,
    VendorLedgerEntryType EntryType,
    decimal Amount,
    Guid? ReferenceId) : DomainEvent;
