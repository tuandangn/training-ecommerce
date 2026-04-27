using MediatR;
using NamEcommerce.Application.Contracts.Communication;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

/// <summary>
/// Khi phiếu giao hàng được duyệt — gửi notification ra n8n để báo cho team giao nhận.
/// </summary>
public sealed class DeliveryNoteConfirmedEventHandler : INotificationHandler<DeliveryNoteConfirmed>
{
    private readonly IN8nAppService _n8nAppService;

    public DeliveryNoteConfirmedEventHandler(IN8nAppService n8nAppService)
    {
        _n8nAppService = n8nAppService;
    }

    public async Task Handle(DeliveryNoteConfirmed notification, CancellationToken cancellationToken)
    {
        await _n8nAppService.NotifyDeliveryNoteIsConfirmed(notification.DeliveryNoteId).ConfigureAwait(false);
    }
}
