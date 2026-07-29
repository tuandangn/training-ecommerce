namespace NamEcommerce.Domain.Shared.Events.Orders;

public sealed record OrderPlaced(
    Guid OrderId, string OrderCode, Guid CustomerId, decimal OrderTotal) : DomainEvent;

public sealed record OrderInfoUpdated(Guid OrderId) : DomainEvent;

public sealed record OrderCancelled(Guid OrderId,
    IReadOnlyCollection<OrderReservationItem> Items) : DomainEvent, IReliableDomainEvent;

public sealed record OrderDeleted(Guid OrderId, string OrderCode,
    IReadOnlyCollection<OrderReservationItem> Items) : DomainEvent, IReliableDomainEvent;


public sealed record OrderReservationItem(Guid ProductId, decimal Quantity);

public sealed record OrderCompleted(Guid OrderId) : DomainEvent, IReliableDomainEvent;

public sealed record OrderShippingUpdated(Guid OrderId) : DomainEvent;

public sealed record OrderFullyDelivered(Guid OrderId, Guid CustomerId) : DomainEvent, IReliableDomainEvent;

public sealed record OrderHasPayment(Guid OrderId, decimal PaidAmount, Guid? PaymentItentId) : DomainEvent, IReliableDomainEvent;


public sealed record DeliveryRequested(
    Guid OrderId, Guid DeliveryNoteId, DateTime RequestedAtUtc) : DomainEvent, IReliableDomainEvent;


public sealed record OrderItemAdded(
    Guid OrderId, Guid OrderItemId, Guid ProductId, decimal Quantity,
    decimal UnitPrice, string? ProductName) : DomainEvent, IReliableDomainEvent;

public sealed record OrderItemUpdated(
    Guid OrderId, Guid OrderItemId, Guid ProductId,
    decimal OldQuantity, decimal Quantity, decimal UnitPrice,
    decimal OldUnitPrice, string? ProductName) : DomainEvent, IReliableDomainEvent;

public sealed record OrderItemRemoved(
    Guid OrderId, Guid OrderItemId, Guid ProductId,
    decimal Quantity, decimal UnitPrice, string? ProductName) : DomainEvent, IReliableDomainEvent;

public sealed record OrderItemDelivered(
    Guid OrderId, Guid OrderItemId, Guid PictureId) : DomainEvent;

