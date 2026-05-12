using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;

namespace NamEcommerce.Application.Contracts.GoodsReceipts;

public interface IGoodsReceiptAppService
{
    Task<GoodsReceiptAppDto?> GetGoodsReceiptByIdAsync(Guid id);
    Task<IPagedDataAppDto<GoodsReceiptAppDto>> GetGoodsReceiptsAsync(int pageIndex, int pageSize, string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc);

    /// <summary>Lấy tất cả phiếu nhận hàng đã gắn với một đơn nhập.</summary>
    Task<IList<GoodsReceiptAppDto>> GetGoodsReceiptsByPurchaseOrderIdAsync(Guid purchaseOrderId);

    Task<CommonActionResultDto> SetGoodsReceiptToPurchaseOrder(SetGoodsReceiptToPurchaseOrderAppDto dto);
    Task<CommonActionResultDto> RemoveGoodsReceiptFromPurchaseOrder(RemoveGoodsReceiptFromPurchaseOrderAppDto dto);
    Task<IList<SuggestedPurchaseOrderForGoodsReceiptAppDto>> GetSuggestedPurchaseOrdersAsync(Guid goodsReceiptId);

    Task<CreateGoodsReceiptResultAppDto> CreateGoodsReceiptAsync(CreateGoodsReceiptAppDto dto);
    Task<UpdateGoodsReceiptResultAppDto> UpdateGoodsReceiptAsync(UpdateGoodsReceiptAppDto dto);
    Task<(bool success, string? errorMessage)> DeleteGoodsReceiptAsync(Guid id);

    Task<SetGoodsReceiptItemUnitCostResultAppDto> SetGoodsReceiptItemUnitCostAsync(SetGoodsReceiptItemUnitCostAppDto dto);
    Task<SetGoodsReceiptVendorResultAppDto> SetGoodsReceiptVendorAsync(SetGoodsReceiptVendorAppDto dto);
}
