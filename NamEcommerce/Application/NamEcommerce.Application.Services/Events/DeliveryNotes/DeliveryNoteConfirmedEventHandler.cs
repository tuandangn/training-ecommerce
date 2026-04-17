using MediatR;
using NamEcommerce.Application.Contracts.Communication;
using NamEcommerce.Application.Contracts.Events.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

public sealed class DeliveryNoteConfirmedEventHandler : INotificationHandler<DeliveryNoteConfirmedNotification>
{
    private readonly IN8nAppService _n8nAppService;

    public DeliveryNoteConfirmedEventHandler(IN8nAppService n8nAppService)
    {
        _n8nAppService = n8nAppService;
    }

    public async Task Handle(DeliveryNoteConfirmedNotification notification, CancellationToken cancellationToken)
    {
        await _n8nAppService.NotifyDeliveryNoteIsConfirmed(notification.Id).ConfigureAwait(false);
    }
}
