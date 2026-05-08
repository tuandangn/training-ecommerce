using NamEcommerce.Domain.Shared.Dtos.Returns;

namespace NamEcommerce.Domain.Shared.Services.Returns;

public interface ICustomerReturnManager
{
    Task<CustomerReturnDto> CreateAsync(CreateCustomerReturnDto dto, Guid? createdByUserId);
    Task<CustomerReturnDto> UpdateAsync(UpdateCustomerReturnDto dto);
    Task MoveToInspectingAsync(Guid id);
    Task ConfirmAsync(Guid id);
    Task CancelAsync(Guid id);

    Task<CustomerReturnDto?> GetByIdAsync(Guid id);
    Task<(int Total, List<CustomerReturnDto> Items)> GetListAsync(Guid? customerId, Guid? orderId, int? status, int pageIndex, int pageSize);

    /// <summary>
    /// Tính tổng <c>AcceptedQuantity</c> đã trả cho một (orderId, productId) qua các phiếu Confirmed.
    /// Dùng để validate không vượt quá số đã giao.
    /// </summary>
    Task<decimal> GetTotalConfirmedReturnQuantityAsync(Guid orderId, Guid productId, Guid? excludeReturnId = null);

    /// <summary>
    /// Gọi sau khi <c>GoodsReceipt</c> được sinh từ <c>CustomerReturnConfirmedEventHandler</c>:
    /// <list type="number">
    ///   <item><description>Ghi nhận <c>GeneratedGoodsReceiptId</c> lên phiếu trả (idempotency guard).</description></item>
    ///   <item><description>Giảm <c>CustomerDebt</c> của Order theo FIFO <c>CreatedOnUtc</c> — có thể xuống âm.</description></item>
    /// </list>
    /// Chỉ dành cho <c>CustomerReturnConfirmedEventHandler</c> gọi.
    /// </summary>
    Task FinalizeConfirmAsync(Guid returnId, Guid generatedGoodsReceiptId, decimal totalReturnAmount);
}
