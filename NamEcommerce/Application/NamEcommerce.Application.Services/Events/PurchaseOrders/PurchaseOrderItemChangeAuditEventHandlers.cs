using MediatR;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class PurchaseOrderItemAddedAuditEventHandler(
    IPurchaseOrderItemChangeAuditManager auditManager,
    ICurrentUserAccessor currentUserAccessor) : INotificationHandler<PurchaseOrderItemAdded>
{
    public async Task Handle(PurchaseOrderItemAdded notification, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        await auditManager.RecordAsync(new CreatePurchaseOrderItemChangeAuditDto
        {
            PurchaseOrderId = notification.PurchaseOrderId,
            PurchaseOrderItemId = notification.PurchaseOrderItemId,
            ProductId = notification.ProductId,
            ProductName = GetProductName(notification.ProductName, notification.ProductId),
            Action = PurchaseOrderItemChangeAuditAction.Added,
            NewQuantity = notification.QuantityOrdered,
            NewUnitCost = notification.UnitCost,
            NewNote = notification.Note,
            ChangedByUserId = currentUser?.Id,
            ChangedByUsername = currentUser?.Username
        }).ConfigureAwait(false);
    }

    private static string GetProductName(string? productName, Guid productId)
        => string.IsNullOrWhiteSpace(productName) ? productId.ToString() : productName;
}

public sealed class PurchaseOrderItemUpdatedAuditEventHandler(
    IPurchaseOrderItemChangeAuditManager auditManager,
    ICurrentUserAccessor currentUserAccessor) : INotificationHandler<PurchaseOrderItemUpdated>
{
    public async Task Handle(PurchaseOrderItemUpdated notification, CancellationToken cancellationToken)
    {
        if (notification.OldQuantityOrdered == notification.QuantityOrdered
            && notification.OldUnitCost == notification.UnitCost
            && string.Equals(notification.OldNote, notification.Note, StringComparison.Ordinal))
        {
            return;
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        await auditManager.RecordAsync(new CreatePurchaseOrderItemChangeAuditDto
        {
            PurchaseOrderId = notification.PurchaseOrderId,
            PurchaseOrderItemId = notification.PurchaseOrderItemId,
            ProductId = notification.ProductId,
            ProductName = GetProductName(notification.ProductName, notification.ProductId),
            Action = PurchaseOrderItemChangeAuditAction.Updated,
            OldQuantity = notification.OldQuantityOrdered,
            NewQuantity = notification.QuantityOrdered,
            OldUnitCost = notification.OldUnitCost,
            NewUnitCost = notification.UnitCost,
            OldNote = notification.OldNote,
            NewNote = notification.Note,
            ChangedByUserId = currentUser?.Id,
            ChangedByUsername = currentUser?.Username
        }).ConfigureAwait(false);
    }

    private static string GetProductName(string? productName, Guid productId)
        => string.IsNullOrWhiteSpace(productName) ? productId.ToString() : productName;
}

public sealed class PurchaseOrderItemRemovedAuditEventHandler(
    IPurchaseOrderItemChangeAuditManager auditManager,
    ICurrentUserAccessor currentUserAccessor) : INotificationHandler<PurchaseOrderItemRemoved>
{
    public async Task Handle(PurchaseOrderItemRemoved notification, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        await auditManager.RecordAsync(new CreatePurchaseOrderItemChangeAuditDto
        {
            PurchaseOrderId = notification.PurchaseOrderId,
            PurchaseOrderItemId = notification.PurchaseOrderItemId,
            ProductId = notification.ProductId,
            ProductName = GetProductName(notification.ProductName, notification.ProductId),
            Action = PurchaseOrderItemChangeAuditAction.Removed,
            OldQuantity = notification.QuantityOrdered,
            OldUnitCost = notification.UnitCost,
            OldNote = notification.Note,
            ChangedByUserId = currentUser?.Id,
            ChangedByUsername = currentUser?.Username
        }).ConfigureAwait(false);
    }

    private static string GetProductName(string? productName, Guid productId)
        => string.IsNullOrWhiteSpace(productName) ? productId.ToString() : productName;
}
