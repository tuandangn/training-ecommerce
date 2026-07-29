using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

namespace NamEcommerce.Application.Contracts.PurchaseOrders;

public interface IPurchaseOrderAppService
{
    Task<IPagedDataAppDto<PurchaseOrderAppDto>> GetPurchaseOrdersAsync(int pageIndex, int pageSize, string? keywords, int? status);

    Task<PurchaseOrderAppDto?> GetPurchaseOrderByIdAsync(Guid id);

    Task<PurchaseOrderAppDto?> GetPurchaseOrderByCodeAsync(string code);

    Task<CreatePurchaseOrderResultAppDto> CreatePurchaseOrderAsync(CreatePurchaseOrderAppDto dto);
    Task<QuickCreatePurchaseOrderResultAppDto> QuickCreatePurchaseOrderAsync(QuickCreatePurchaseOrderAppDto dto);
    Task<CreatePurchaseOrderResultAppDto> CopyPurchaseOrderAsync(Guid id);
    Task<CreatePurchaseOrderResultAppDto> SplitPurchaseOrderAsync(SplitPurchaseOrderAppDto dto);

    Task<UpdatePurchaseOrderResultAppDto> UpdatePurchaseOrderAsync(UpdatePurchaseOrderAppDto dto);

    Task<CommonActionResultDto> SubmitsPurchaseOrderAsync(Guid id);

    Task<CommonActionResultDto> CancelPurchaseOrderAsync(Guid id);

    Task<CommonActionResultDto> ClosePartialPurchaseOrderAsync(Guid id, string reason);

    Task<CommonActionResultDto> ApprovePurchaseOrderAsync(Guid id);

    Task<CommonActionResultDto> ChangeStatusAsync(Guid purchaseOrderId, int newStatus);

    Task<CommonActionResultDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemAppDto dto);

    Task<CommonActionResultDto> UpdatePurchaseOrderItemAsync(UpdatePurchaseOrderItemAppDto dto);

    Task<CommonActionResultDto> DeletePurchaseOrderItemAsync(DeletePurchaseOrderItemAppDto dto);

    Task<ReceiveItemResultAppDto> ReceiveItemAsync(ReceivedGoodsForItemAppDto dto);

    Task<BulkReceiveGoodsResultAppDto> BulkReceiveAsync(BulkReceiveGoodsAppDto dto);

    Task<CommonActionResultDto> AddReceiptFeesAsync(Guid purchaseOrderId, decimal additionalShipping, decimal additionalTax);

    Task<CommonActionResultDto> SetGoodsReceiptToPurchaseOrderAsync(SetGoodsReceiptToPurchaseOrderAppDto dto);

    Task<CommonActionResultDto> RemoveGoodsReceiptFromPurchaseOrderAsync(RemoveGoodsReceiptFromPurchaseOrderAppDto dto);

    Task<IList<RecentPurchasePriceAppDto>> GetRecentPurchasePricesAsync(Guid productId);

    Task<IList<OrderAllocatedPurchaseOrderAppDto>> GetAllocatedPurchaseOrdersForOrderAsync(Guid orderId);

    Task<IList<EligibleOrderItemForAllocationAppDto>> GetEligibleOrderItemsForPoItemAsync((Guid purchaseOrderId, Guid purchaseOrderItemId) purchaseOrderItemId);

    Task<IList<NonDirectShipAllocationForPoItemAppDto>> GetNonDirectShipAllocationsForPoItemAsync((Guid primaryItemId, Guid secondaryItemId) purchaseOrderItemId);

    Task<decimal> GetAllocationRemainingQuantityAsync(Guid allocationId);

    Task<IList<PurchaseOrderItemAllocationForPoItemAppDto>> GetAllocationsForPurchaseOrderItemsAsync(IReadOnlyList<(Guid primaryItemId, Guid secondaryItemId)> purchaseOrderItemIds);

    Task<decimal> GetMaxAllocationQuantityForOrderItemAsync(Guid orderId, Guid orderItemId);

    Task<CommonActionResultDto> AllocatePoItemForOrderItemAsync(AllocatePoItemForOrderItemAppDto dto);

    Task<CommonActionResultDto> ReleasePoItemAllocationForOrderItemAsync(ReleaseAllocationsOfPurchaseOrderItemAppDto dto);
}
