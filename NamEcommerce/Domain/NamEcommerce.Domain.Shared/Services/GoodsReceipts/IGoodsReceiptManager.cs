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
    /// Nếu UnitCost đã có, cũng trigger <c>GoodsReceiptItemUnitCostSetHandler</c> cập nhật AverageCost.
    /// </summary>
    Task<CreateGoodsReceiptResultDto> CreateFromPurchaseOrderReceivingAsync(CreateGoodsReceiptFromPurchaseOrderDto dto);

    Task SetGoodsReceiptItemUnitCostAsync(SetGoodsReceiptItemUnitCostDto dto);
    Task<SetGoodsReceiptVendorResultDto> SetGoodsReceiptVendorAsync(SetGoodsReceiptVendorDto dto);

    Task RemoveGoodsReceiptFromPurchaseOrder(RemoveGoodsReceiptFromPurchaseOrderDto dto);
    Task SetGoodsReceiptToPurchaseOrder(SetGoodsReceiptToPurchaseOrderDto dto);
    Task<IList<SuggestedPurchaseOrderForGoodsReceiptDto>> GetSuggestedPurchaseOrdersAsync(Guid goodsReceiptId);

    Task<GoodsReceiptDto?> GetGoodsReceiptByIdAsync(Guid id);
    Task<IPagedDataDto<GoodsReceiptDto>> GetGoodsReceiptsAsync(int pageIndex, int pageSize, string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc);
}
