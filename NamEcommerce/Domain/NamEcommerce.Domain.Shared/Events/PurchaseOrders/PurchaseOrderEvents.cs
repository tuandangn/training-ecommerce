using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Events.PurchaseOrders;

public sealed record PurchaseOrderCreated(
    Guid PurchaseOrderId,
    string Code,
    Guid VendorId,
    Guid? WarehouseId) : DomainEvent;

public sealed record PurchaseOrderUpdated(Guid PurchaseOrderId) : DomainEvent;

public sealed record PurchaseOrderCancelled(Guid PurchaseOrderId) : DomainEvent;

public sealed record PurchaseOrderStatusChanged(
    Guid PurchaseOrderId,
    PurchaseOrderStatus OldStatus,
    PurchaseOrderStatus NewStatus) : DomainEvent;

public sealed record PurchaseOrderItemAdded(
    Guid PurchaseOrderId,
    Guid PurchaseOrderItemId,
    Guid ProductId,
    decimal QuantityOrdered,
    decimal UnitCost) : DomainEvent;

public sealed record PurchaseOrderItemRemoved(Guid PurchaseOrderId, Guid PurchaseOrderItemId) : DomainEvent;

public sealed record PurchaseOrderItemReceived(
    Guid PurchaseOrderId,
    Guid PurchaseOrderItemId,
    decimal ReceivedQuantity,
    Guid? GoodsReceiptId = null) : DomainEvent;

public sealed record PurchaseOrderBulkReceived(Guid PurchaseOrderId) : DomainEvent;
