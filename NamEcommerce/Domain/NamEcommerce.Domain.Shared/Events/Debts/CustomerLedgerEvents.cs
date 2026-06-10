using NamEcommerce.Domain.Shared.Enums.Debts;

namespace NamEcommerce.Domain.Shared.Events.Debts;

public sealed record CustomerLedgerEntryRecorded(
    Guid EntryId,
    Guid CustomerId,
    CustomerLedgerEntryType EntryType,
    decimal Amount,
    Guid? ReferenceId) : DomainEvent;
