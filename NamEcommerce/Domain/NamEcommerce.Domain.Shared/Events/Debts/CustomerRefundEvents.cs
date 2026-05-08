namespace NamEcommerce.Domain.Shared.Events.Debts;

/// <summary>
/// Phiếu hoàn tiền khách hàng vừa được tạo (do trả hàng vượt mức nợ còn lại).
/// </summary>
public sealed record CustomerRefundCreated(
    Guid CustomerRefundId,
    Guid CustomerId,
    decimal Amount) : DomainEvent;

/// <summary>
/// Phiếu hoàn tiền đã được thực hiện — tiền đã trả lại cho khách.
/// </summary>
public sealed record CustomerRefundCompleted(
    Guid CustomerRefundId,
    Guid CustomerId,
    decimal Amount) : DomainEvent;

/// <summary>
/// Phiếu hoàn tiền bị huỷ — không thực hiện hoàn trả.
/// </summary>
public sealed record CustomerRefundCancelled(Guid CustomerRefundId) : DomainEvent;
