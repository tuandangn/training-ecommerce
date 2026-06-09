namespace NamEcommerce.Domain.Shared.Events.Returns;

/// <summary>
/// Phiếu trả hàng NCC vừa được xác nhận (Inspecting → Confirmed). Handler subscribe event này để:
/// <list type="number">
///   <item><description>Tạo <c>DeliveryNote(SourceType=ToVendorReturn, OrderId=null)</c> ở trạng thái
///     <c>Delivered</c> ngay — skip Reserve/Confirm/Delivering; trừ tồn thực sự.</description></item>
///   <item><description>Giảm <c>VendorDebt</c> của GoodsReceipt/PurchaseOrder tương ứng (FIFO) bằng
///     <c>ApplyReturn(AcceptedTotal, returnId)</c>.</description></item>
///   <item><description>Set <c>VendorReturn.GeneratedDeliveryNoteId</c> sau khi phiếu xuất được sinh.</description></item>
/// </list>
/// </summary>
public sealed record VendorReturnConfirmed(
    Guid VendorReturnId,
    Guid? PurchaseOrderId,
    Guid? GoodsReceiptId,
    Guid VendorId,
    Guid WarehouseId) : DomainEvent;

/// <summary>
/// Phiếu trả hàng NCC bị huỷ (Draft/Inspecting → Cancelled).
/// Hiện không có handler — event để audit/tracking.
/// </summary>
public sealed record VendorReturnCancelled(Guid VendorReturnId) : DomainEvent;

/// <summary>
/// Tổng giá trị trả hàng NCC vượt quá tổng nợ còn lại — NCC cần hoàn tiền mặt lại cho shop.
/// Handler subscribe event này để tạo <c>VendorRefund</c> với <c>Amount = OverAmount</c>.
/// </summary>
public sealed record VendorReturnOverRecovered(
    Guid VendorReturnId,
    Guid VendorId,
    decimal OverAmount) : DomainEvent;
