namespace NamEcommerce.Domain.Shared.Events.Debts;

/// <summary>
/// Phiếu công nợ khách hàng vừa được tạo (sinh từ DeliveryNote.MarkDelivered).
/// </summary>
public sealed record CustomerDebtCreated(
    Guid CustomerDebtId,
    Guid CustomerId,
    decimal TotalAmount,
    Guid DeliveryNoteId,
    Guid OrderId) : DomainEvent;

/// <summary>
/// Phiếu công nợ khách hàng được cập nhật (apply payment, đổi due date).
/// </summary>
public sealed record CustomerDebtUpdated(Guid CustomerDebtId) : DomainEvent;

/// <summary>
/// Phiếu công nợ khách hàng đã được trả hết (fully paid).
/// </summary>
public sealed record CustomerDebtFullyPaid(Guid CustomerDebtId, Guid CustomerId) : DomainEvent;
