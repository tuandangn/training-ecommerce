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
/// Hiện không có handler — event để audit/tracking trạng thái giao hàng.
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
/// Phiếu giao hàng bị huỷ.
/// Hiện không có handler — stock release và cascade cancel CustomerReturn đang xử lý trực tiếp trong DeliveryNoteManager.CancelAsync.
/// Giữ event để audit/tracking; tách side-effect sang handler là refactor dài hạn.
/// </summary>
public sealed record DeliveryNoteCancelled(
    Guid DeliveryNoteId,
    bool WasReservingStock) : DomainEvent;

// ── Direct-Ship Events ──────────────────────────────────────────────────────

/// <summary>
/// Khách đã xác nhận nhận hàng giao thẳng — Invoice/Debt module subscribe để sinh phải thu.
/// </summary>
public sealed record DirectShipDeliveryConfirmed(
    Guid DeliveryNoteId,
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount) : DomainEvent;

/// <summary>
/// Khách từ chối / hàng giao thất bại — Inventory module subscribe để chuyển stock kho ảo → kho chính.
/// Giá vốn = giá PO của allocation (không phải bình quân).
/// </summary>
public sealed record DirectShipDeliveryRejected(
    Guid DeliveryNoteId,
    Guid OrderId,
    Guid? SourceGoodsReceiptId) : DomainEvent;
