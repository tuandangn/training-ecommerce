using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
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

    Task<BulkReceiveGoodsForPurchaseOrderResultDto> BulkReceiveItemsAsync(BulkReceiveGoodsForPurchaseOrderDto dto);

    Task SetGoodsReceiptToPurchaseOrderAsync(SetGoodsReceiptToPurchaseOrderDto dto);

    Task RemoveGoodsReceiptFromPurchaseOrderAsync(RemoveGoodsReceiptFromPurchaseOrderDto dto);

    Task AddReceiptFeesAsync(Guid purchaseOrderId, decimal additionalShipping, decimal additionalTax);

    Task ChangeStatusAsync(Guid purchaseOrderId, PurchaseOrderStatus status);

    Task ClosePartialAsync(Guid purchaseOrderId, string reason);
    Task VerifyStatusAsync(Guid purchaseOrderId);

    Task<bool> CanChangeStatusToAsync(Guid purchaseOrderId, PurchaseOrderStatus status);
    Task<bool> CanAddPurchaseOrderItemsAsync(Guid purchaseOrderId);
    Task<bool> CanReceiveGoodsAsync(Guid purchaseOrderId);

    Task AcceptOversupplyToMainWarehouseAsync(
        Guid purchaseOrderId,
        Guid purchaseOrderItemId,
        decimal oversupplyQuantity,
        Guid warehouseId,
        CancellationToken ct = default);
}
