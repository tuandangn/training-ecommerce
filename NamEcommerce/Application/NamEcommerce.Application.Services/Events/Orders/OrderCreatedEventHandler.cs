using MediatR;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderCreatedEventHandler : INotificationHandler<EntityCreatedNotification<Order>>
{
    private readonly IOrderManager _orderManager;

    public OrderCreatedEventHandler(IOrderManager orderManager)
    {
        _orderManager = orderManager;
    }

    public Task Handle(EntityCreatedNotification<Order> notification, CancellationToken cancellationToken)
    {
        //*TODO*
        //// Reserve Stock if warehouse is specified
        //if (order.WarehouseId.HasValue)
        //{
        //    foreach (var item in order.OrderItems)
        //    {
        //        // ReferenceId is order.Id, userId is Guid.Empty (system) for now
        //        await _stockManager.ReserveStockAsync(item.ProductId, order.WarehouseId.Value, item.Quantity, order.Id, Guid.Empty, "Sales Order Reservation").ConfigureAwait(false);
        //    }
        //}

        return Task.CompletedTask;
    }
}
