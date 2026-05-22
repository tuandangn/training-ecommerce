using MediatR;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Outbox;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

/// <summary>
/// Khi phiếu giao hàng được duyệt — enqueue <see cref="DeliveryNoteConfirmedIntegrationEvent"/>
/// vào Outbox để background service publish ra n8n (thay vì call trực tiếp).
/// <para>
/// Lý do dùng Outbox:
/// <list type="bullet">
///   <item>Atomic với transaction nghiệp vụ — nếu SaveChanges fail thì không leak notification ra ngoài.</item>
///   <item>n8n down tạm thời không làm fail business operation.</item>
///   <item>Retry tự động khi gọi n8n thất bại.</item>
/// </list>
/// </para>
/// </summary>
public sealed class DeliveryNoteConfirmedEventHandler : INotificationHandler<DeliveryNoteConfirmed>
{
    private readonly IOutbox _outbox;

    public DeliveryNoteConfirmedEventHandler(IOutbox outbox)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        _outbox = outbox;
    }

    public Task Handle(DeliveryNoteConfirmed notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var integrationEvent = new DeliveryNoteConfirmedIntegrationEvent(notification.DeliveryNoteId);
        return _outbox.AddAsync(integrationEvent, cancellationToken);
    }
}
