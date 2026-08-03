using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderItemUpdatedEventReservationHandler(
    IProductReservationManager productReservationManager) : INotificationHandler<OrderItemUpdated>
{
    public async Task Handle(OrderItemUpdated notification, CancellationToken cancellationToken)
    {
        var deltaQuantity = notification.Quantity - notification.OldQuantity;
        await productReservationManager.AdjustAsync(
            notification.ProductId,
            deltaQuantity,
            notification.OrderId,
            ProductReservationReason.OrderItemIncreased,
            ProductReservationReason.OrderItemDecreased,
            notification.OrderItemId);
    }
}

public sealed class OrderItemUpdatedEventScheduleHandler(
    IOrderFulfillmentScheduleAppService orderFulfillmentScheduleAppService,
    IOrderAppService orderAppService, IOrderManager orderManager) : INotificationHandler<OrderItemUpdated>
{
    public async Task Handle(OrderItemUpdated notification, CancellationToken cancellationToken)
    {
        var order = await orderManager.GetOrderByIdAsync(notification.OrderId).ConfigureAwait(false);
        if (order is null)
            return;
        if (order is { Status: OrderStatus.Completed or OrderStatus.Cancelled })
            return;

        var orderItem = order.Items.FirstOrDefault(item => item.Id == notification.OrderItemId);
        if (orderItem is null)
            return;

        var activeScheduledQuantity = await orderFulfillmentScheduleAppService.GetActiveScheduledQuantityForOrderItemAsync(order.Id, orderItem.Id).ConfigureAwait(false);
        if (activeScheduledQuantity == 0)
            return;

        var newRemainingQuantity = await orderAppService.GetRemainShippingQuantityForOrderItemAsync(order.Id, orderItem.Id).ConfigureAwait(false);
        if (activeScheduledQuantity <= newRemainingQuantity)
            return;

        var schedules = await orderFulfillmentScheduleAppService.GetByOrderIdAsync(order.Id, false).ConfigureAwait(false);
        foreach (var schedule in schedules.Where(schedule => schedule.Items.Any(item => item.OrderItemId == orderItem.Id)))
        {
            await orderFulfillmentScheduleAppService.SetActiveAsync(new SetOrderFulfillmentScheduleActiveAppDto(schedule.Id, false));
        }
    }
}
