using NamEcommerce.Application.Contracts.Dtos.Returns;

namespace NamEcommerce.Application.Contracts.Returns;

public interface IVendorReturnAppService
{
    Task<CreateVendorReturnResultAppDto> CreateAsync(CreateVendorReturnAppDto dto);
    Task<UpdateVendorReturnResultAppDto> UpdateAsync(UpdateVendorReturnAppDto dto);
    Task<ConfirmVendorReturnResultAppDto> MoveToInspectingAsync(Guid id);
    Task<ConfirmVendorReturnResultAppDto> ConfirmAsync(Guid id, Guid? warehouseId = null);
    Task<ConfirmVendorReturnResultAppDto> CancelAsync(Guid id);
    Task<ConfirmVendorReturnResultAppDto> ReverseConfirmedAsync(Guid id, string reason);

    Task<VendorReturnAppDto?> GetByIdAsync(Guid id);
    Task<(int Total, List<VendorReturnAppDto> Items)> GetListAsync(
        int pageIndex, int pageSize, Guid? vendorId = null,
        Guid? purchaseOrderId = null, Guid? goodsReceiptId = null,
        int? status = null);

    /// <summary>Lấy danh sách phiếu nhập kho (FromVendor) của một NCC — cho AJAX picker. Có thể lọc theo PurchaseOrderId.</summary>
    Task<List<GoodsReceiptPickerAppDto>> GetGoodsReceiptsByVendorAsync(Guid vendorId, Guid? purchaseOrderId = null);

    /// <summary>Lấy danh sách items có thể trả của một phiếu nhập kho — bao gồm số lượng đã trả.</summary>
    Task<List<ReturnableItemAppDto>> GetGoodsReceiptItemsForReturnAsync(Guid goodsReceiptId, Guid? excludeReturnId = null);

    /// <summary>
    /// Lọc danh sách Warehouse có đủ tồn (QuantityAvailable) để đáp ứng TOÀN BỘ items đang trả.
    /// Kho hợp lệ = có >= RequiredQty cho mọi (ProductId).
    /// </summary>
    Task<List<Guid>> GetWarehousesWithSufficientStockAsync(IReadOnlyList<(Guid ProductId, decimal RequiredQty)> items);
}
