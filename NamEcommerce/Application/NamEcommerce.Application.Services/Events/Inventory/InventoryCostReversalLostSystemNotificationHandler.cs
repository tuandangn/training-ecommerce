using MediatR;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Application.Services.Notifications;
using NamEcommerce.Domain.Shared.Events.Inventory;

namespace NamEcommerce.Application.Services.Events.Inventory;

public sealed class InventoryCostReversalLostSystemNotificationHandler(
    ISystemNotificationAppService notificationAppService)
    : INotificationHandler<InventoryCostReturnReversalLost>
{
    public async Task Handle(InventoryCostReturnReversalLost notification, CancellationToken cancellationToken)
    {
        if (notification is null || notification.AffectedDeliveryNoteIds.Count == 0)
            return;

        await notificationAppService
            .CreateAsync(InventoryCostSystemNotificationComposer.ReturnReversalLost(
                notification.ProductId,
                notification.AffectedDeliveryNoteIds.Count))
            .ConfigureAwait(false);
    }
}
