using MediatR;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

/// <summary>
/// Handler cho <see cref="DirectShipDeliveryRejected"/>.
/// Legacy compatibility: flow hiện tại chuyển stock và hủy DN trực tiếp trong DirectShipManager.
/// </summary>
public sealed class DirectShipDeliveryRejectedHandler : INotificationHandler<DirectShipDeliveryRejected>
{
    public Task Handle(DirectShipDeliveryRejected notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
