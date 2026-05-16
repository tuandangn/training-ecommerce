using MediatR;
using NamEcommerce.Application.Contracts.Communication;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

/// <summary>
/// Handler cho <see cref="DeliveryNoteConfirmedIntegrationEvent"/> — thực hiện cuộc gọi đến n8n.
/// <para>
/// Được dispatch bởi <c>OutboxProcessor</c> background service (không chạy trong transaction nghiệp vụ).
/// Nếu n8n trả lỗi, exception sẽ bubble lên để OutboxProcessor có thể retry sau.
/// </para>
/// </summary>
public sealed class DeliveryNoteConfirmedIntegrationEventHandler
    : INotificationHandler<DeliveryNoteConfirmedIntegrationEvent>
{
    private readonly IN8nAppService _n8nAppService;

    public DeliveryNoteConfirmedIntegrationEventHandler(IN8nAppService n8nAppService)
    {
        ArgumentNullException.ThrowIfNull(n8nAppService);
        _n8nAppService = n8nAppService;
    }

    public Task Handle(DeliveryNoteConfirmedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return _n8nAppService.NotifyDeliveryNoteIsConfirmed(notification.DeliveryNoteId);
    }
}
