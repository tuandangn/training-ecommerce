using MediatR;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderCancelledEventHandler(IDeliveryNoteManager deliveryNoteManager) : INotificationHandler<OrderCancelled>
{
    public async Task Handle(OrderCancelled notification, CancellationToken cancellationToken)
    {
        var deliveryNotes = await deliveryNoteManager.GetDeliveryNotesAsync(0, int.MaxValue, orderId: notification.OrderId).ConfigureAwait(false);
        foreach (var deliveryNote in deliveryNotes)
        {
            if (deliveryNote.Status != DeliveryNoteStatus.Draft)
                continue;

            await deliveryNoteManager.CancelAsync(deliveryNote.Id).ConfigureAwait(false);
        }
    }
}
