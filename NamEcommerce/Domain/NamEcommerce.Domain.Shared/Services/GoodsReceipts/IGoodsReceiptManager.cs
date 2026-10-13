using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

namespace NamEcommerce.Domain.Shared.Services.GoodsReceipts;

public interface IGoodsReceiptManager
{
    Task<CreateGoodsReceiptResultDto> CreateGoodsReceiptAsync(CreateGoodsReceiptDto dto);
    Task<UpdateGoodsReceiptResultDto> UpdateGoodsReceiptAsync(UpdateGoodsReceiptDto dto);
    Task DeleteGoodsReceiptAsync(DeleteGoodsReceiptDto dto);

    Task<Guid> CreateFromVendorOversupplyAsync(CreateGoodsReceiptFromVendorOversupplyDto dto);

    /// <summary>
    /// Tự động tạo GoodsReceipt khi CustomerReturn được Confirm (SourceType=FromCustomerReturn).
    /// Không sinh VendorDebt. Chỉ cộng tồn kho qua GoodsReceiptCreatedHandler (có guard SourceType).
    /// UnitCost được phục hồi từ allocation gốc nếu đã có, nếu chưa có thì để pending.
    /// </summary>
    Task<Guid> CreateFromCustomerReturnAsync(CreateGoodsReceiptFromCustomerReturnDto dto);

    /// <summary>
    /// Tạo phiếu nhập tồn kho đầu kỳ (SourceType=OpeningBalance) khi tạo sản phẩm mới.
    /// UnitCost bắt buộc &gt; 0. Phiếu này không thể hủy, xóa, hoặc tạo trả NCC.
    /// </summary>
    Task<CreateGoodsReceiptResultDto> CreateForOpeningInventoryAsync(CreateOpeningInventoryReceiptDto dto);

    Task SetGoodsReceiptItemUnitCostAsync(SetGoodsReceiptItemUnitCostDto dto);
    Task<SetGoodsReceiptVendorResultDto> SetGoodsReceiptVendorAsync(SetGoodsReceiptVendorDto dto);

    Task<IList<SuggestedPurchaseOrderForGoodsReceiptDto>> GetSuggestedPurchaseOrdersAsync(Guid goodsReceiptId);

    Task<GoodsReceiptDto?> GetGoodsReceiptByIdAsync(Guid id);
    Task<IPagedDataDto<GoodsReceiptDto>> GetGoodsReceiptsAsync(int pageIndex, int pageSize, string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc);

    /// <summary>Lấy toàn bộ phiếu nhận hàng đã được gắn vào một đơn nhập (không phân trang).</summary>
    Task<IList<GoodsReceiptDto>> GetGoodsReceiptsByPurchaseOrderIdAsync(Guid purchaseOrderId);
}
