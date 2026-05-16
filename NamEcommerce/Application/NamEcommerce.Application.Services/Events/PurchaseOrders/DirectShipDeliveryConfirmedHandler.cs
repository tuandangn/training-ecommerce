using MediatR;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

/// <summary>
/// Handler cho <see cref="DirectShipDeliveryConfirmed"/>.
/// D2.7: Sau khi khách xác nhận nhận hàng giao thẳng — trigger sinh Invoice/CustomerDebt.
/// Stock kho ảo đã "tiêu thụ" — không cần chuyển thêm.
/// </summary>
public sealed class DirectShipDeliveryConfirmedHandler : INotificationHandler<DirectShipDeliveryConfirmed>
{
    public Task Handle(DirectShipDeliveryConfirmed notification, CancellationToken cancellationToken)
        => Task.CompletedTask; // D2.7: implement CustomerDebt creation
}
