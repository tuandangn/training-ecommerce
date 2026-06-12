namespace NamEcommerce.Domain.Shared.Events.StockTransfer;

public sealed record StockTransferNoteApproved(Guid NoteId, Guid FromWarehouseId, Guid ToWarehouseId) : DomainEvent, IReliableDomainEvent;
