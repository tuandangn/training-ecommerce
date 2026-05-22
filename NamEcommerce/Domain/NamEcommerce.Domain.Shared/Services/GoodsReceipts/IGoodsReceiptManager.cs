using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

namespace NamEcommerce.Domain.Shared.Services.GoodsReceipts;

public interface IGoodsReceiptManager
{
    Task<CreateGoodsReceiptResultDto> CreateGoodsReceiptAsync(CreateGoodsReceiptDto dto);
    Task<UpdateGoodsReceiptResultDto> UpdateGoodsReceiptAsync(UpdateGoodsReceiptDto dto);
    Task DeleteGoodsReceiptAsync(DeleteGoodsReceiptDto dto);

    /// <summary>
    /// Tự động tạo GoodsReceipt khi nhận 1 item từ PurchaseOrder.
    /// Chỉ dành cho <c>PurchaseOrderManager.ReceiveItemsAsync</c> — không phải flow thủ công.
    /// MarkCreated sẽ trigger <c>GoodsReceiptCreatedHandler</c> cộng tồn + sinh VendorDebt.
    /// Nếu UnitCost đã có, cũng trigger <c>GoodsReceiptItemUnitCostSetHandler</c> để chốt inventory cost layer.
    /// </summary>
    Task<CreateGoodsReceiptResultDto> CreateFromPurchaseOrderReceivingAsync(CreateGoodsReceiptFromPurchaseOrderDto dto);

    /// <summary>
    /// Tự động tạo 1 GoodsReceipt cho nhiều items cùng nhập vào 1 kho từ 1 PurchaseOrder.
    /// Dùng cho <c>PurchaseOrderManager.BulkReceiveItemsAsync</c> — caller đã group lines theo WarehouseId trước khi gọi.
    /// MarkCreated chạy 1 lần (thay vì N) → handler cộng tồn cho từng item + sinh VendorDebt 1 lần.
    /// </summary>
    Task<CreateGoodsReceiptResultDto> CreateBulkFromPurchaseOrderReceivingAsync(CreateGoodsReceiptFromPurchaseOrderBulkDto dto);

    /// <summary>
    /// Tự động tạo GoodsReceipt khi CustomerReturn được Confirm (SourceType=FromCustomerReturn).
    /// Không sinh VendorDebt. Chỉ cộng tồn kho qua GoodsReceiptCreatedHandler (có guard SourceType).
    /// UnitCost được phục hồi từ allocation gốc nếu đã có, nếu chưa có thì để pending.
    /// </summary>
    Task<Guid> CreateFromCustomerReturnAsync(CreateGoodsReceiptFromCustomerReturnDto dto);

    Task SetGoodsReceiptItemUnitCostAsync(SetGoodsReceiptItemUnitCostDto dto);
    Task<SetGoodsReceiptVendorResultDto> SetGoodsReceiptVendorAsync(SetGoodsReceiptVendorDto dto);

    Task<IList<SuggestedPurchaseOrderForGoodsReceiptDto>> GetSuggestedPurchaseOrdersAsync(Guid goodsReceiptId);

    Task<GoodsReceiptDto?> GetGoodsReceiptByIdAsync(Guid id);
    Task<IPagedDataDto<GoodsReceiptDto>> GetGoodsReceiptsAsync(int pageIndex, int pageSize, string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc);

    /// <summary>Lấy toàn bộ phiếu nhận hàng đã được gắn vào một đơn nhập (không phân trang).</summary>
    Task<IList<GoodsReceiptDto>> GetGoodsReceiptsByPurchaseOrderIdAsync(Guid purchaseOrderId);
}
