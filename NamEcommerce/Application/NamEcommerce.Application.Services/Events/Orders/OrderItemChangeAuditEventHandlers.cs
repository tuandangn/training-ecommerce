using MediatR;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderItemAddedAuditEventHandler(
    IOrderItemChangeAuditManager auditManager,
    ICurrentUserAccessor currentUserAccessor) : INotificationHandler<OrderItemAdded>
{
    public async Task Handle(OrderItemAdded notification, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        await auditManager.RecordAsync(new CreateOrderItemChangeAuditDto
        {
            OrderId = notification.OrderId,
            OrderItemId = notification.OrderItemId,
            ProductId = notification.ProductId,
            ProductName = GetProductName(notification.ProductName, notification.ProductId),
            Action = OrderItemChangeAuditAction.Added,
            NewQuantity = notification.Quantity,
            NewUnitPrice = notification.UnitPrice,
            ChangedByUserId = currentUser?.Id,
            ChangedByUsername = currentUser?.Username
        }).ConfigureAwait(false);
    }

    private static string GetProductName(string? productName, Guid productId)
        => string.IsNullOrWhiteSpace(productName) ? productId.ToString() : productName;
}

public sealed class OrderItemUpdatedAuditEventHandler(
    IOrderItemChangeAuditManager auditManager,
    ICurrentUserAccessor currentUserAccessor) : INotificationHandler<OrderItemUpdated>
{
    public async Task Handle(OrderItemUpdated notification, CancellationToken cancellationToken)
    {
        if (notification.OldQuantity == notification.Quantity && notification.OldUnitPrice == notification.UnitPrice)
            return;

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        await auditManager.RecordAsync(new CreateOrderItemChangeAuditDto
        {
            OrderId = notification.OrderId,
            OrderItemId = notification.OrderItemId,
            ProductId = notification.ProductId,
            ProductName = GetProductName(notification.ProductName, notification.ProductId),
            Action = OrderItemChangeAuditAction.Updated,
            OldQuantity = notification.OldQuantity,
            NewQuantity = notification.Quantity,
            OldUnitPrice = notification.OldUnitPrice,
            NewUnitPrice = notification.UnitPrice,
            ChangedByUserId = currentUser?.Id,
            ChangedByUsername = currentUser?.Username
        }).ConfigureAwait(false);
    }

    private static string GetProductName(string? productName, Guid productId)
        => string.IsNullOrWhiteSpace(productName) ? productId.ToString() : productName;
}

public sealed class OrderItemRemovedAuditEventHandler(
    IOrderItemChangeAuditManager auditManager,
    ICurrentUserAccessor currentUserAccessor) : INotificationHandler<OrderItemRemoved>
{
    public async Task Handle(OrderItemRemoved notification, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        await auditManager.RecordAsync(new CreateOrderItemChangeAuditDto
        {
            OrderId = notification.OrderId,
            OrderItemId = notification.OrderItemId,
            ProductId = notification.ProductId,
            ProductName = GetProductName(notification.ProductName, notification.ProductId),
            Action = OrderItemChangeAuditAction.Removed,
            OldQuantity = notification.Quantity,
            OldUnitPrice = notification.UnitPrice,
            ChangedByUserId = currentUser?.Id,
            ChangedByUsername = currentUser?.Username
        }).ConfigureAwait(false);
    }

    private static string GetProductName(string? productName, Guid productId)
        => string.IsNullOrWhiteSpace(productName) ? productId.ToString() : productName;
}
