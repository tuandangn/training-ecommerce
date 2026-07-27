using MediatR;
using NamEcommerce.Application.Contracts.Communication;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Outbox;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

public sealed class DeliveryNoteConfirmedHandler(
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IOutbox outbox) : INotificationHandler<DeliveryNoteConfirmed>
{
    public async Task Handle(DeliveryNoteConfirmed notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        var deliveryNote = await deliveryNoteDataReader.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return;

        if (deliveryNote.IsDirectShip)
            return;

        if (deliveryNote.SourceType != DeliveryNoteSourceType.ToCustomer && deliveryNote.SourceType != DeliveryNoteSourceType.DirectShipToCustomer)
            return;

        var integrationEvent = new DeliveryNoteConfirmedIntegrationEvent(notification.DeliveryNoteId);
        await outbox.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class DeliveryNoteConfirmedIntegrationHandler
    : INotificationHandler<DeliveryNoteConfirmedIntegrationEvent>
{
    private readonly IN8nAppService _n8nAppService;

    public DeliveryNoteConfirmedIntegrationHandler(IN8nAppService n8nAppService)
    {
        ArgumentNullException.ThrowIfNull(n8nAppService);
        _n8nAppService = n8nAppService;
    }

    public async Task Handle(DeliveryNoteConfirmedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        await _n8nAppService.NotifyDeliveryNoteIsConfirmed(notification.DeliveryNoteId).ConfigureAwait(false);
    }
}
