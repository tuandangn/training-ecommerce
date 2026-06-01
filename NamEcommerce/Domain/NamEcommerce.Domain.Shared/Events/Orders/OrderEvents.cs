namespace NamEcommerce.Domain.Shared.Events.Orders;

/// <summary>
/// Đơn bán hàng vừa được tạo.
/// Handler hiện tại reserve tồn global cho toàn bộ item của đơn.
/// </summary>
public sealed record OrderPlaced(
    Guid OrderId,
    string OrderCode,
    Guid CustomerId,
    decimal OrderTotal) : DomainEvent;

/// <summary>
/// Thông tin chung của đơn (note, expected shipping date, discount...) được cập nhật.
/// Hiện không có handler — event để audit/tracking.
/// </summary>
public sealed record OrderInfoUpdated(Guid OrderId) : DomainEvent;

/// <summary>
/// Một dòng hàng được thêm vào đơn.
/// Handler hiện tại tăng global reservation theo số lượng dòng hàng.
/// </summary>
public sealed record OrderItemAdded(
    Guid OrderId,
    Guid OrderItemId,
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    string? ProductName) : DomainEvent;

/// <summary>
/// Một dòng hàng trong đơn được sửa số lượng / đơn giá.
/// Handler hiện tại adjust global reservation theo delta số lượng.
/// </summary>
public sealed record OrderItemUpdated(
    Guid OrderId,
    Guid OrderItemId,
    Guid ProductId,
    decimal OldQuantity,
    decimal Quantity,
    decimal UnitPrice,
    decimal OldUnitPrice,
    string? ProductName) : DomainEvent;

/// <summary>
/// Một dòng hàng bị xoá khỏi đơn.
/// Handler hiện tại release global reservation theo số lượng dòng hàng.
/// </summary>
public sealed record OrderItemRemoved(
    Guid OrderId,
    Guid OrderItemId,
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    string? ProductName) : DomainEvent;

/// <summary>
/// Đơn đã được kết sổ và hoàn thành.
/// Handler hiện tại release phần global reservation còn lại của đơn.
/// </summary>
public sealed record OrderCompleted(Guid OrderId) : DomainEvent;

/// <summary>
/// Thông tin giao hàng (shipping address / expected shipping date) được cập nhật.
/// Hiện không có handler — event để audit/tracking.
/// </summary>
public sealed record OrderShippingUpdated(Guid OrderId) : DomainEvent;

/// <summary>
/// Một dòng hàng đã được đánh dấu là đã giao (kèm ảnh chứng minh).
/// Hiện không có handler — event để audit/tracking; DeliveryNoteDelivered xử lý stock/debt liên quan.
/// </summary>
public sealed record OrderItemDelivered(
    Guid OrderId,
    Guid OrderItemId,
    Guid PictureId) : DomainEvent;

/// <summary>
/// Đơn bị huỷ. Handler hiện tại release toàn bộ global reservation còn lại của đơn.
/// </summary>
public sealed record OrderCancelled(
    Guid OrderId,
    IReadOnlyCollection<OrderReservationItem> Items) : DomainEvent;

/// <summary>
/// Đơn bị xoá (soft delete).
/// Handler hiện tại release toàn bộ global reservation còn lại của đơn.
/// </summary>
public sealed record OrderDeleted(
    Guid OrderId,
    string OrderCode,
    IReadOnlyCollection<OrderReservationItem> Items) : DomainEvent;

/// <summary>
/// Snapshot số lượng cần release/reserve theo product tại thời điểm event được raise.
/// </summary>
public sealed record OrderReservationItem(Guid ProductId, decimal Quantity);
