using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Domain.Shared.Enums.Notifications;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderFullyDeliveredEventHandler(
    IOrderManager orderManager,
    ISystemNotificationAppService notificationService) : INotificationHandler<OrderFullyDelivered>
{
    public async Task Handle(OrderFullyDelivered notification, CancellationToken cancellationToken)
    {
        var order = await orderManager.GetOrderByIdAsync(notification.OrderId).ConfigureAwait(false);
        if (order is null) return;

        await notificationService.CreateAsync(new CreateSystemNotificationAppDto
        {
            Type = (int)SystemNotificationType.DeliveryNoteDelivered,
            Severity = (int)SystemNotificationSeverity.Info,
            Title = "Đơn hàng đã giao đủ",
            Message = $"Đơn {order.Code} đã giao đủ hàng — kiểm tra đối soát tiền trước khi tất toán.",
            RequiredPermission = "Orders.Manage",
            RelatedEntityId = notification.OrderId,
            RelatedEntityType = "Order",
            ActionUrl = $"/Orders/Detail?id={notification.OrderId}"
        }).ConfigureAwait(false);
    }
}
