using MediatR;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

/// <summary>
/// Subscribe concrete <see cref="OrderPlaced"/> event (raised từ <c>Order</c> entity khi đơn được tạo).
/// Migrated từ <c>EntityCreatedNotification&lt;Order&gt;</c> trong session 2026-05-02 / Phase 5 prerequisite.
/// Reserve tồn global cho từng product ngay sau khi đơn được lưu.
/// </summary>
public sealed class OrderCreatedEventHandler : INotificationHandler<OrderPlaced>
{
    private readonly IOrderManager _orderManager;
    private readonly IProductReservationManager _productReservationManager;

    public OrderCreatedEventHandler(
        IOrderManager orderManager,
        IProductReservationManager productReservationManager)
    {
        _orderManager = orderManager;
        _productReservationManager = productReservationManager;
    }

    public async Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        var order = await _orderManager.GetOrderByIdAsync(notification.OrderId).ConfigureAwait(false);
        if (order is null) return;

        foreach (var itemGroup in order.Items.GroupBy(item => item.ProductId))
        {
            await _productReservationManager.ReserveAsync(
                itemGroup.Key,
                itemGroup.Sum(item => item.Quantity),
                order.Id).ConfigureAwait(false);
        }
    }
}
