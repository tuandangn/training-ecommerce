namespace NamEcommerce.Domain.Shared.Events.PurchaseOrders;

/// <summary>
/// Allocation được đánh dấu là direct-ship — audit log + cảnh báo tái in phiếu.
/// </summary>
public sealed record AllocationMarkedAsDirectShip(
    Guid AllocationId,
    Guid PurchaseOrderItemId,
    string Address,
    string? ContactName,
    string? ContactPhone) : DomainEvent;

/// <summary>
/// GoodsReceipt phân bổ hàng cho direct-ship → tạo DN PendingConfirmation.
/// Inventory module subscribe để stock-in kho ảo DirectShipTransit.
/// </summary>
public sealed record DirectShipDeliveryPending(
    Guid AllocationId,
    Guid DeliveryNoteId,
    Guid GoodsReceiptId,
    Guid WarehouseTransitId,
    decimal Quantity) : DomainEvent;

/// <summary>
/// SO bị huỷ khi còn allocation FullyReceived ở kho ảo.
/// Inventory module subscribe để chuyển stock kho ảo → kho chính (giá vốn = giá PO).
/// </summary>
public sealed record SoCancelledWithDirectShipReceived(
    Guid OrderId,
    IReadOnlyList<Guid> AllocationIds) : DomainEvent;

/// <summary>
/// Địa chỉ giao thẳng của allocation bị sửa — audit log + cảnh báo tái in phiếu NCC.
/// </summary>
public sealed record DirectShipAddressUpdated(
    Guid AllocationId,
    string OldAddress,
    string NewAddress,
    Guid EditedByUserId) : DomainEvent;

/// <summary>
/// NCC giao thừa và user chọn "Nhập kho chính" cho phần thừa.
/// Inventory module subscribe để stock-in kho chính + tăng công nợ NCC.
/// </summary>
public sealed record VendorOversupplyAccepted(
    Guid PurchaseOrderItemId,
    Guid WarehouseId,
    decimal OversupplyQuantity,
    decimal UnitCost) : DomainEvent;
