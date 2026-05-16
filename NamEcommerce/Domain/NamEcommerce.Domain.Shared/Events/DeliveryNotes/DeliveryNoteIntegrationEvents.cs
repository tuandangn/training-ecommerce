namespace NamEcommerce.Domain.Shared.Events.DeliveryNotes;

/// <summary>
/// Integration event — phiếu giao hàng đã được duyệt, cần thông báo ra hệ thống ngoài (n8n)
/// để team giao nhận chuẩn bị xuất kho.
/// <para>
/// Được enqueue vào Outbox trong cùng transaction với business operation duyệt phiếu;
/// background service đọc Outbox sẽ publish event này qua MediatR và handler sẽ gọi n8n.
/// </para>
/// </summary>
public sealed record DeliveryNoteConfirmedIntegrationEvent(Guid DeliveryNoteId) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
