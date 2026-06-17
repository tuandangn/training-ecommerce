namespace NamEcommerce.Domain.Shared.Events.DeliveryNotes;

/// <summary>
/// Phiếu giao hàng vừa được tạo (status = Draft).
/// </summary>
public sealed record DeliveryNoteCreated(
    Guid DeliveryNoteId,
    Guid OrderId,
    Guid CustomerId) : DomainEvent, IReliableDomainEvent;

/// <summary>
/// Phiếu giao hàng đã được duyệt — stock được reserve cho phiếu này.
/// </summary>
public sealed record DeliveryNoteConfirmed(Guid DeliveryNoteId) : DomainEvent;

/// <summary>
/// Phiếu giao hàng đang trong tình trạng giao (Confirmed → Delivering).
/// Handler <c>DeliveryNoteDeliveringStockHandler</c>: <c>DispatchStockUpToAsync</c> (trừ tồn kho) + <c>RegisterOutboundAsync</c> (ghi giá vốn).
/// </summary>
public sealed record DeliveryNoteDelivering(Guid DeliveryNoteId) : DomainEvent, IReliableDomainEvent;

/// <summary>
/// Phiếu giao hàng đã giao thành công — stock đã trừ, đơn hàng đã đánh dấu item delivered, sẵn sàng sinh công nợ.
/// <para><c>AmountToCollect</c> = tiền thu tại chỗ (chỉ phần khách nhận). <c>DebtAmount</c> = công nợ ghi sổ
/// (gồm cả phần khách trả lại lúc giao — phần này sẽ được credit khi CustomerReturn tự sinh được Confirm).</para>
/// </summary>
public sealed record DeliveryNoteDelivered(
    Guid DeliveryNoteId,
    Guid OrderId,
    Guid CustomerId,
    decimal AmountToCollect,
    decimal DebtAmount) : DomainEvent, IReliableDomainEvent;

/// <summary>
/// Phiếu giao hàng bị huỷ.
/// Hiện không có handler — stock release và cascade cancel CustomerReturn đang xử lý trực tiếp trong DeliveryNoteManager.CancelAsync.
/// Giữ event để audit/tracking; tách side-effect sang handler là refactor dài hạn.
/// </summary>
public sealed record DeliveryNoteCancelled(
    Guid DeliveryNoteId,
    bool WasReservingStock) : DomainEvent;

// ── Settlement Approval Events ───────────────────────────────────────────────

/// <summary>Shipper gửi duyệt thu hụt (trả hàng / khách từ chối thanh toán) — chờ admin.</summary>
public sealed record DeliverySettlementApprovalRequested(
    Guid DeliveryNoteId,
    Guid OrderId,
    string Code) : DomainEvent;

/// <summary>Admin duyệt thu hụt — shipper được thu đúng số đã duyệt.</summary>
public sealed record DeliverySettlementApproved(
    Guid DeliveryNoteId,
    Guid OrderId,
    string Code,
    decimal ApprovedAmountToCollect) : DomainEvent;

/// <summary>Admin từ chối — hàng mang về, phiếu hủy.</summary>
public sealed record DeliverySettlementRejected(
    Guid DeliveryNoteId,
    Guid OrderId,
    string Code,
    string Reason) : DomainEvent;

// ── Direct-Ship Events ──────────────────────────────────────────────────────

/// <summary>
/// Legacy direct-ship event. Flow hiện tại dùng <see cref="DeliveryNoteDelivered"/>.
/// </summary>
public sealed record DirectShipDeliveryConfirmed(
    Guid DeliveryNoteId,
    Guid OrderId,
    Guid CustomerId,
    decimal AmountToCollect) : DomainEvent;

/// <summary>
/// Legacy direct-ship event. Flow hiện tại xử lý reject trong DirectShipManager rồi raise <see cref="DeliveryNoteCancelled"/>.
/// </summary>
public sealed record DirectShipDeliveryRejected(
    Guid DeliveryNoteId,
    Guid OrderId,
    Guid? SourceGoodsReceiptId) : DomainEvent;
