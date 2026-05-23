using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.Inventory;
using NamEcommerce.Domain.Shared.Events.Orders;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record ProductReservationLedger : AppAggregateEntity
{
    internal ProductReservationLedger(
        Guid productId,
        Guid orderId,
        decimal quantityDelta,
        ProductReservationReason reason,
        Guid? referenceId) : base(Guid.NewGuid())
    {
        ProductId = productId;
        OrderId = orderId;
        QuantityDelta = quantityDelta;
        Reason = reason;
        ReferenceId = referenceId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; init; }
    public Guid OrderId { get; init; }
    public decimal QuantityDelta { get; init; }
    public ProductReservationReason Reason { get; init; }
    public Guid? ReferenceId { get; init; }
    public DateTime CreatedOnUtc { get; init; }

    internal void MarkAsCreated() => RaiseDomainEvent(new ProductReservationLedgerCreated(Id, ProductId, OrderId, QuantityDelta));
}
