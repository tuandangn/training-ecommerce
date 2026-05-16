using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Services.PurchaseOrders;

public interface IPurchaseOrderManager : ICodeExistCheckingService
{
    Task<IPagedDataDto<PurchaseOrderDto>> GetPurchaseOrdersAsync(int pageIndex, int pageSize, string? keywords, PurchaseOrderStatus? status);
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id);
    Task<PurchaseOrderDto?> GetPurchaseOrderByCodeAsync(string code);

    Task<IList<RecentPurchasePriceDto>> GetRecentPurchasePricesAsync(Guid productId);
    Task<ExistingDraftPurchaseOrderDto?> FindDraftForVendorAsync(Guid vendorId);
    Task<IList<RelatedPurchaseOrderDto>> FindRelatedPurchaseOrdersAsync(Guid vendorId, IList<Guid> productIds, IList<PurchaseOrderStatus> statuses);

    Task<CreatePurchaseOrderResultDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto);
    Task<CreatePoFromShortageResultDto> CreatePurchaseOrderFromShortageAsync(CreatePoFromShortageDto dto);
    Task<CreatePoFromShortageResultDto> AddItemsToExistingDraftAsync(Guid purchaseOrderId, IList<CreatePoFromShortageItemDto> items);
    Task<UpdatePurchaseOrderResultDto> UpdatePurchaseOrderAsync(UpdatePurchaseOrderDto dto);
    
    Task<AddPurchaseOrderItemResultDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemDto dto);
    Task DeleteOrderItemAsync(Guid purchaseOrderId, Guid itemId);
    Task<ReceivedGoodsForItemResultDto> ReceiveItemsAsync(ReceivedGoodsForItemDto dto);

    /// <summary>
    /// Nhận nhiều items cùng 1 lần. Lines được group theo WarehouseId — mỗi group sinh 1 GoodsReceipt.
    /// Cộng dồn QuantityReceived cho từng PO item, validate aggregate qty không vượt ordered,
    /// 1 lần UpdateAsync PO ở cuối. Trả về danh sách GoodsReceipt ids đã tạo.
    /// </summary>
    Task<BulkReceiveGoodsForPurchaseOrderResultDto> BulkReceiveItemsAsync(BulkReceiveGoodsForPurchaseOrderDto dto);

    /// <summary>Cộng dồn phí vận chuyển và tiền thuế vào đơn nhập sau khi nhận hàng.</summary>
    Task AddReceiptFeesAsync(Guid purchaseOrderId, decimal additionalShipping, decimal additionalTax);

    Task ChangeStatusAsync(Guid purchaseOrderId, PurchaseOrderStatus status);

    Task ClosePartialAsync(Guid purchaseOrderId, string reason);
    Task VerifyStatusAsync(Guid purchaseOrderId);

    Task<bool> CanChangeStatusToAsync(Guid purchaseOrderId, PurchaseOrderStatus status);
    Task<bool> CanAddPurchaseOrderItemsAsync(Guid purchaseOrderId);
    Task<bool> CanReceiveGoodsAsync(Guid purchaseOrderId);
}
