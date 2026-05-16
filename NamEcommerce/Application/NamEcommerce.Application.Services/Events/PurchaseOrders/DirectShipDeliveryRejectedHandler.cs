using MediatR;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

/// <summary>
/// Handler cho <see cref="DirectShipDeliveryRejected"/>.
/// D2.8: Khách từ chối nhận hàng — chuyển stock kho ảo DirectShipTransit → kho chính, giá vốn = giá PO.
/// </summary>
public sealed class DirectShipDeliveryRejectedHandler : INotificationHandler<DirectShipDeliveryRejected>
{
    public Task Handle(DirectShipDeliveryRejected notification, CancellationToken cancellationToken)
        => Task.CompletedTask; // D2.8: implement stock transfer kho ảo → kho chính
}
