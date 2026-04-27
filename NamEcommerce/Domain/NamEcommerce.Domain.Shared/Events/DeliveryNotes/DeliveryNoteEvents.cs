namespace NamEcommerce.Domain.Shared.Events.DeliveryNotes;

/// <summary>
/// Phiếu giao hàng vừa được tạo (status = Draft).
/// </summary>
public sealed record DeliveryNoteCreated(
    Guid DeliveryNoteId,
    Guid OrderId,
    Guid CustomerId) : DomainEvent;

/// <summary>
/// Phiếu giao hàng đã được duyệt — stock được reserve cho phiếu này.
/// </summary>
public sealed record DeliveryNoteConfirmed(Guid DeliveryNoteId) : DomainEvent;

/// <summary>
/// Phiếu giao hàng đang trong tình trạng giao (Confirmed → Delivering).
/// </summary>
public sealed record DeliveryNoteDelivering(Guid DeliveryNoteId) : DomainEvent;

/// <summary>
/// Phiếu giao hàng đã giao thành công — stock đã trừ, đơn hàng đã đánh dấu item delivered, sẵn sàng sinh công nợ.
/// </summary>
public sealed record DeliveryNoteDelivered(
    Guid DeliveryNoteId,
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount) : DomainEvent;

/// <summary>
/// Phiếu giao hàng bị huỷ — stock đã reserve cần được release nếu phiếu trước đó là Confirmed/Delivering.
/// </summary>
public sealed record DeliveryNoteCancelled(
    Guid DeliveryNoteId,
    bool WasReservingStock) : DomainEvent;
