using MediatR;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderItemRemovedAllocationReleaseHandler(
    IPurchaseOrderAllocationManager allocationManager) : INotificationHandler<OrderItemRemoved>
{
    public Task Handle(OrderItemRemoved notification, CancellationToken cancellationToken)
        => allocationManager.ReleaseAllocationsForOrderItemAsync(notification.OrderItemId);
}

public sealed class OrderCancelledAllocationReleaseHandler(
    IPurchaseOrderAllocationManager allocationManager) : INotificationHandler<OrderCancelled>
{
    public Task Handle(OrderCancelled notification, CancellationToken cancellationToken)
        => allocationManager.ReleaseAllocationsForOrderAsync(notification.OrderId);
}

public sealed class OrderDeletedAllocationReleaseHandler(
    IPurchaseOrderAllocationManager allocationManager) : INotificationHandler<OrderDeleted>
{
    public Task Handle(OrderDeleted notification, CancellationToken cancellationToken)
        => allocationManager.ReleaseAllocationsForOrderAsync(notification.OrderId);
}
