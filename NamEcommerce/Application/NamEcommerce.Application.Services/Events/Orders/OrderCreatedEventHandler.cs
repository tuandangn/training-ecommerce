using MediatR;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

/// <summary>
/// Subscribe concrete <see cref="OrderPlaced"/> event (raised từ <c>Order</c> entity khi đơn được tạo).
/// Migrated từ <c>EntityCreatedNotification&lt;Order&gt;</c> trong session 2026-05-02 / Phase 5 prerequisite.
/// Body hiện tại trả <see cref="Task.CompletedTask"/> — Reserve Stock vẫn là TODO chưa implement
/// (xem comment dưới). Khi muốn implement, kéo từ <c>OrderPlaced</c> những thông tin cần thiết
/// (OrderId, CustomerId, OrderTotal); để query items + warehouse phải dùng <c>IOrderManager</c> /
/// <c>IEntityDataReader&lt;Order&gt;</c> như trước.
/// </summary>
public sealed class OrderCreatedEventHandler : INotificationHandler<OrderPlaced>
{
    private readonly IOrderManager _orderManager;

    public OrderCreatedEventHandler(IOrderManager orderManager)
    {
        _orderManager = orderManager;
    }

    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        //*TODO*
        //// Reserve Stock if warehouse is specified
        //// var order = await _orderManager.GetOrderAsync(notification.OrderId).ConfigureAwait(false);
        //// if (order.WarehouseId.HasValue)
        //// {
        ////     foreach (var item in order.OrderItems)
        ////     {
        ////         // ReferenceId is order.Id, userId is Guid.Empty (system) for now
        ////         await _stockManager.ReserveStockAsync(item.ProductId, order.WarehouseId.Value, item.Quantity, order.Id, Guid.Empty, "Sales Order Reservation").ConfigureAwait(false);
        ////     }
        //// }

        return Task.CompletedTask;
    }
}
