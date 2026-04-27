namespace NamEcommerce.Domain.Shared.Events.Orders;

/// <summary>
/// Đơn bán hàng vừa được tạo.
/// </summary>
public sealed record OrderPlaced(
    Guid OrderId,
    string OrderCode,
    Guid CustomerId,
    decimal OrderTotal) : DomainEvent;

/// <summary>
/// Thông tin chung của đơn (note, expected shipping date, discount...) được cập nhật.
/// </summary>
public sealed record OrderInfoUpdated(Guid OrderId) : DomainEvent;

/// <summary>
/// Một dòng hàng được thêm vào đơn.
/// </summary>
public sealed record OrderItemAdded(
    Guid OrderId,
    Guid OrderItemId,
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice) : DomainEvent;

/// <summary>
/// Một dòng hàng trong đơn được sửa số lượng / đơn giá.
/// </summary>
public sealed record OrderItemUpdated(
    Guid OrderId,
    Guid OrderItemId,
    decimal Quantity,
    decimal UnitPrice) : DomainEvent;

/// <summary>
/// Một dòng hàng bị xoá khỏi đơn.
/// </summary>
public sealed record OrderItemRemoved(Guid OrderId, Guid OrderItemId) : DomainEvent;

/// <summary>
/// Đơn bị khoá (manual hoặc auto-lock khi tất cả item đã giao).
/// </summary>
public sealed record OrderLocked(Guid OrderId, string? Reason) : DomainEvent;

/// <summary>
/// Thông tin giao hàng (shipping address / expected shipping date) được cập nhật.
/// </summary>
public sealed record OrderShippingUpdated(Guid OrderId) : DomainEvent;

/// <summary>
/// Một dòng hàng đã được đánh dấu là đã giao (kèm ảnh chứng minh).
/// </summary>
public sealed record OrderItemDelivered(
    Guid OrderId,
    Guid OrderItemId,
    Guid PictureId) : DomainEvent;

/// <summary>
/// Đơn bị xoá (soft delete).
/// </summary>
public sealed record OrderDeleted(Guid OrderId, string OrderCode) : DomainEvent;
