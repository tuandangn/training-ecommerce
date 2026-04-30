using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Events.PurchaseOrders;

/// <summary>
/// Đơn nhập hàng vừa được tạo (status = Draft).
/// </summary>
public sealed record PurchaseOrderCreated(
    Guid PurchaseOrderId,
    string Code,
    Guid VendorId,
    Guid? WarehouseId) : DomainEvent;

/// <summary>
/// Thông tin chung của đơn nhập (note, expected delivery, vendor, warehouse, tax, shipping...) được cập nhật.
/// </summary>
public sealed record PurchaseOrderUpdated(Guid PurchaseOrderId) : DomainEvent;

/// <summary>
/// Trạng thái đơn nhập thay đổi (Draft → Submitted → Approved → Receiving → Completed, hoặc Cancelled).
/// </summary>
public sealed record PurchaseOrderStatusChanged(
    Guid PurchaseOrderId,
    PurchaseOrderStatus OldStatus,
    PurchaseOrderStatus NewStatus) : DomainEvent;

/// <summary>
/// Một dòng hàng được thêm vào đơn nhập.
/// </summary>
public sealed record PurchaseOrderItemAdded(
    Guid PurchaseOrderId,
    Guid PurchaseOrderItemId,
    Guid ProductId,
    decimal QuantityOrdered,
    decimal UnitCost) : DomainEvent;

/// <summary>
/// Một dòng hàng bị xoá khỏi đơn nhập.
/// </summary>
public sealed record PurchaseOrderItemRemoved(Guid PurchaseOrderId, Guid PurchaseOrderItemId) : DomainEvent;

/// <summary>
/// Một dòng hàng vừa được nhận hàng (cộng <c>QuantityReceived</c>).
/// Handler subscribe event này để tự verify và chuyển trạng thái đơn (Approved → Receiving → Completed)
/// thay cho cách cũ đi qua <c>EntityUpdatedNotification&lt;PurchaseOrder&gt;</c>.
/// </summary>
public sealed record PurchaseOrderItemReceived(
    Guid PurchaseOrderId,
    Guid PurchaseOrderItemId,
    decimal ReceivedQuantity) : DomainEvent;
