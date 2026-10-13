using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Services.PurchaseOrders;

public interface IPurchaseOrderAllocationManager
{
    Task<PurchaseOrderItemAllocationDto> AllocatePurchaseOrderItemForOrder(AllocatePurchaseOrderItemForOrder dto);
    Task<DistributeReceivedQuantityResultDto> SyncReceivedForPurchaseOrderItemAsync(Guid purchaseOrderItemId, decimal purchaseOrderItemReceivedQuantity);
    Task<IList<PurchaseOrderItemAllocationDto>> GetAllocationsForPurchaseOrderItemAsync(Guid purchaseOrderItemId);
    Task ReleaseAllocationsOfPurchaseOrderItemAsync(SecondaryItemId purchaseOrderItemId);
    Task ReleaseAllocationsForOrderItemAsync(Guid orderItemId);
    Task ReleaseAllocationsForOrderAsync(Guid orderId);
    Task<IList<OrderAllocatedPurchaseOrderDto>> GetAllocatedPurchaseOrdersForOrderAsync(Guid orderId);
    Task<IList<EligibleOrderItemForAllocationDto>> GetEligibleOrderItemsForPoItemAsync(SecondaryItemId purchaseOrderItemId);
    Task<IList<NonDirectShipAllocationDto>> GetNonDirectShipAllocationsForPoItemAsync(SecondaryItemId purchaseOrderItemId);
}
