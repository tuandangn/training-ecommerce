namespace NamEcommerce.Domain.Shared.Events.Returns;

/// <summary>
/// Phiếu trả hàng khách vừa được xác nhận (Inspecting → Confirmed). Handler subscribe event này để:
/// <list type="number">
///   <item><description>Tạo <c>GoodsReceipt(SourceType=FromCustomerReturn)</c> nhận lại hàng trả.</description></item>
///   <item><description>Giảm <c>CustomerDebt</c> của Order tương ứng theo FIFO <c>CreatedOnUtc</c> bằng
///     <c>ApplyReturn(AcceptedTotal, returnId)</c> — có thể xuống âm.</description></item>
///   <item><description>Set <c>CustomerReturn.GeneratedGoodsReceiptId</c> sau khi phiếu nhập được sinh.</description></item>
/// </list>
/// </summary>
public sealed record CustomerReturnConfirmed(
    Guid CustomerReturnId,
    Guid OrderId,
    Guid CustomerId,
    Guid WarehouseId) : DomainEvent;

/// <summary>
/// Phiếu trả hàng khách bị huỷ (Draft/Inspecting → Cancelled).
/// Hiện không có handler — event để audit/tracking.
/// </summary>
public sealed record CustomerReturnCancelled(Guid CustomerReturnId) : DomainEvent;

/// <summary>
/// Tổng giá trị trả hàng vượt quá tổng nợ còn lại — cần hoàn tiền mặt cho khách.
/// Handler subscribe event này để tạo <c>CustomerRefund</c> với <c>Amount = OverAmount</c>.
/// </summary>
public sealed record CustomerReturnOverRefunded(
    Guid CustomerReturnId,
    Guid CustomerId,
    decimal OverAmount,
    Guid OverRefundedDebtId) : DomainEvent;
