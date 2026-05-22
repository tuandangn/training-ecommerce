using MediatR;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

/// <summary>
/// Handler cho <see cref="DirectShipDeliveryConfirmed"/>.
/// Legacy compatibility: flow hiện tại dùng DeliveryNoteDelivered để sinh CustomerDebt và dispatch tồn kho.
/// </summary>
public sealed class DirectShipDeliveryConfirmedHandler : INotificationHandler<DirectShipDeliveryConfirmed>
{
    public Task Handle(DirectShipDeliveryConfirmed notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
