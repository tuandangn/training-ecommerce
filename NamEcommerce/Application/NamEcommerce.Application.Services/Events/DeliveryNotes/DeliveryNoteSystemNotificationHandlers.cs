using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Application.Services.Notifications;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

public sealed class DeliveryNoteSystemNotificationHandlers(
    IDeliveryNoteAppService deliveryNoteAppService,
    ISystemNotificationAppService notificationAppService)
    : INotificationHandler<DeliveryNoteCreated>,
        INotificationHandler<DeliveryNoteConfirmed>,
        INotificationHandler<DeliveryNoteDelivering>,
        INotificationHandler<DeliveryNoteDelivered>
{
    public async Task Handle(DeliveryNoteCreated notification, CancellationToken cancellationToken)
    {
        var note = await GetCustomerDeliveryNoteAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (note is null)
            return;

        await notificationAppService
            .CreateAsync(DeliverySystemNotificationComposer.DeliveryNoteCreated(note))
            .ConfigureAwait(false);
    }

    public async Task Handle(DeliveryNoteConfirmed notification, CancellationToken cancellationToken)
    {
        var note = await GetCustomerDeliveryNoteAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (note is null || note.IsDirectShip || note.SourceType != (int)DeliveryNoteSourceType.ToCustomer)
            return;

        await notificationAppService
            .CreateAsync(DeliverySystemNotificationComposer.DeliveryNoteConfirmed(note))
            .ConfigureAwait(false);
    }

    public async Task Handle(DeliveryNoteDelivering notification, CancellationToken cancellationToken)
    {
        var note = await GetCustomerDeliveryNoteAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (note is null)
            return;

        await notificationAppService
            .CreateAsync(DeliverySystemNotificationComposer.DeliveryNoteDelivering(note))
            .ConfigureAwait(false);
    }

    public async Task Handle(DeliveryNoteDelivered notification, CancellationToken cancellationToken)
    {
        var note = await GetCustomerDeliveryNoteAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (note is null)
            return;

        await notificationAppService
            .CreateAsync(DeliverySystemNotificationComposer.DeliveryNoteDelivered(note))
            .ConfigureAwait(false);
    }

    private async Task<DeliveryNoteAppDto?> GetCustomerDeliveryNoteAsync(Guid deliveryNoteId)
    {
        var note = await deliveryNoteAppService.GetByIdAsync(deliveryNoteId).ConfigureAwait(false);
        if (note is null)
            return null;

        return note.SourceType is (int)DeliveryNoteSourceType.ToCustomer or (int)DeliveryNoteSourceType.DirectShipToCustomer
            ? note
            : null;
    }
}
