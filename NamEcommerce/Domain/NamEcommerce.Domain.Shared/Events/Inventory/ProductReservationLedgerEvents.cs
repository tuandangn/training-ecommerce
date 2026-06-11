namespace NamEcommerce.Domain.Shared.Events.Inventory;

[Serializable]
public sealed record ProductReservationLedgerCreated(Guid ProductReservationLedgerId, Guid ProductId, Guid OrderId, decimal QuantityDelta) : DomainEvent, IReliableDomainEvent;
