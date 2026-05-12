using NamEcommerce.Application.Contracts.Dtos.Returns;

namespace NamEcommerce.Application.Contracts.Returns;

public interface IVendorReturnAppService
{
    Task<CreateVendorReturnResultAppDto> CreateAsync(CreateVendorReturnAppDto dto, Guid? createdByUserId);
    Task<UpdateVendorReturnResultAppDto> UpdateAsync(UpdateVendorReturnAppDto dto);
    Task<ConfirmVendorReturnResultAppDto> MoveToInspectingAsync(Guid id);
    Task<ConfirmVendorReturnResultAppDto> ConfirmAsync(Guid id);
    Task<ConfirmVendorReturnResultAppDto> CancelAsync(Guid id);
    Task<ConfirmVendorReturnResultAppDto> ReverseConfirmedAsync(Guid id, string reason);

    Task<VendorReturnAppDto?> GetByIdAsync(Guid id);
    Task<(int Total, List<VendorReturnAppDto> Items)> GetListAsync(
        Guid? vendorId, Guid? purchaseOrderId, Guid? goodsReceiptId, int? status, int pageIndex, int pageSize);

    /// <summary>Lấy danh sách phiếu nhập kho (FromVendor) của một NCC — cho AJAX picker.</summary>
    Task<List<GoodsReceiptPickerAppDto>> GetGoodsReceiptsByVendorAsync(Guid vendorId);

    /// <summary>Lấy danh sách items có thể trả của một phiếu nhập kho — bao gồm số lượng đã trả.</summary>
    Task<List<ReturnableItemAppDto>> GetGoodsReceiptItemsForReturnAsync(Guid goodsReceiptId, Guid? excludeReturnId = null);
}
