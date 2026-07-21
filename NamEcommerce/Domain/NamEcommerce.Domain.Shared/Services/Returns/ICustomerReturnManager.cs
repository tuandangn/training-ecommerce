using NamEcommerce.Domain.Shared.Dtos.Returns;

namespace NamEcommerce.Domain.Shared.Services.Returns;

public interface ICustomerReturnManager
{
    Task<CustomerReturnDto> CreateAsync(CreateCustomerReturnDto dto);
    Task<CustomerReturnDto> UpdateAsync(UpdateCustomerReturnDto dto);
    Task MoveToInspectingAsync(Guid id);
    Task ConfirmAsync(Guid id, Guid? warehouseId = null);
    Task CancelAsync(Guid id);

    Task<CustomerReturnDto?> GetByIdAsync(Guid id);
    Task<(int Total, List<CustomerReturnDto> Items)> GetListAsync(
        int pageIndex, int pageSize, Guid? customerId = null, Guid? deliveryNoteId = null, int? status = null);

    /// <summary>
    /// Tính tổng <c>AcceptedQuantity</c> đã chiếm chỗ cho một (deliveryNoteId, productId) — bao gồm cả phiếu
    /// <c>Inspecting</c> và <c>Confirmed</c>. Dùng để validate không vượt quá số đã giao.
    /// <para>
    /// Việc gộp <c>Inspecting</c> nhằm thu hẹp cửa sổ race condition khi hai phiếu trả cùng <c>Confirm</c>:
    /// workflow là <c>Draft → Inspecting → Confirmed</c>, nên phiếu đã <c>MoveToInspecting</c> được coi như "đã chiếm chỗ".
    /// Vẫn còn race nhỏ nếu hai phiếu cùng <c>MoveToInspecting</c> đồng thời — giải quyết triệt để cần thêm
    /// concurrency token (RowVersion) — xem ToDoList.md P2 future work.
    /// </para>
    /// </summary>
    Task<decimal> GetTotalReservedReturnQuantityAsync(Guid deliveryNoteId, Guid productId, Guid? excludeReturnId = null);

    /// <summary>
    /// Gọi sau khi <c>GoodsReceipt</c> được sinh từ <c>CustomerReturnConfirmedEventHandler</c>:
    /// <list type="number">
    ///   <item><description>Ghi nhận <c>GeneratedGoodsReceiptId</c> lên phiếu trả (idempotency guard).</description></item>
    ///   <item><description>Giảm <c>CustomerDebt</c> của khách hàng theo FIFO <c>CreatedOnUtc</c> — có thể xuống âm.</description></item>
    ///   <item><description>Nếu <c>AdditionalCost &gt; 0</c>, tạo Expense ghi nhận chi phí phát sinh.</description></item>
    /// </list>
    /// Chỉ dành cho <c>CustomerReturnConfirmedEventHandler</c> gọi.
    /// </summary>
    Task FinalizeConfirmAsync(Guid returnId, Guid generatedGoodsReceiptId, decimal netRefundAmount);
}
