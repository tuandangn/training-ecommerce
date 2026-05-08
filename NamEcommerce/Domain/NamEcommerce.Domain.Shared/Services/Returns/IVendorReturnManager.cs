using NamEcommerce.Domain.Shared.Dtos.Returns;

namespace NamEcommerce.Domain.Shared.Services.Returns;

public interface IVendorReturnManager
{
    Task<VendorReturnDto> CreateAsync(CreateVendorReturnDto dto, Guid? createdByUserId);
    Task<VendorReturnDto> UpdateAsync(UpdateVendorReturnDto dto);
    Task MoveToInspectingAsync(Guid id);
    Task ConfirmAsync(Guid id);
    Task CancelAsync(Guid id);

    Task<VendorReturnDto?> GetByIdAsync(Guid id);
    Task<(int Total, List<VendorReturnDto> Items)> GetListAsync(Guid? vendorId, Guid? purchaseOrderId, Guid? goodsReceiptId, int? status, int pageIndex, int pageSize);

    /// <summary>
    /// Tính tổng <c>AcceptedQuantity</c> đã trả cho một (goodsReceiptId/purchaseOrderId, productId) qua các phiếu Confirmed.
    /// Dùng để validate không vượt quá số đã nhập từ NCC.
    /// </summary>
    Task<decimal> GetTotalConfirmedReturnQuantityAsync(Guid productId, Guid? goodsReceiptId, Guid? purchaseOrderId, Guid? excludeReturnId = null);

    /// <summary>
    /// Gọi sau khi <c>DeliveryNote</c> được sinh từ <c>VendorReturnConfirmedEventHandler</c>:
    /// <list type="number">
    ///   <item><description>Ghi nhận <c>GeneratedDeliveryNoteId</c> lên phiếu trả (idempotency guard).</description></item>
    ///   <item><description>Giảm <c>VendorDebt</c> theo FIFO <c>CreatedOnUtc</c> (lọc theo GoodsReceiptId hoặc PurchaseOrderId) — có thể xuống âm.</description></item>
    /// </list>
    /// Chỉ dành cho <c>VendorReturnConfirmedEventHandler</c> gọi.
    /// </summary>
    Task FinalizeConfirmAsync(Guid returnId, Guid generatedDeliveryNoteId, decimal totalReturnAmount);
}
