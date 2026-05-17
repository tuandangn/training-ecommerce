using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Services.PurchaseOrders;

public interface IPurchaseOrderAllocationManager
{
    Task<PurchaseOrderItemAllocationDto> AllocateAsync(Guid purchaseOrderItemId, Guid orderItemId, decimal quantity);
    Task<PurchaseOrderItemAllocationDto> AllocateFromExistingPurchaseOrderItemAsync(Guid purchaseOrderItemId, Guid orderItemId, decimal quantity);
    Task IncreaseReceivedAsync(Guid allocationId, decimal receivedQty);
    Task<DistributeReceivedQuantityResultDto> SyncReceivedForPurchaseOrderItemAsync(Guid purchaseOrderItemId, decimal purchaseOrderItemReceivedQuantity);
    Task<IList<PurchaseOrderItemAllocationDto>> GetAllocationsForOrderItemAsync(Guid orderItemId);
    Task<IList<PurchaseOrderItemAllocationDto>> GetAllocationsForPurchaseOrderItemAsync(Guid purchaseOrderItemId);
    Task ReleaseAllocationsForOrderItemAsync(Guid orderItemId);
    Task ReleaseAllocationsForOrderAsync(Guid orderId);
    Task<IList<OrderAllocatedPurchaseOrderDto>> GetAllocatedPurchaseOrdersForOrderAsync(Guid orderId);
    Task<IList<EligibleOrderItemForAllocationDto>> GetEligibleOrderItemsForPoItemAsync(Guid purchaseOrderItemId);
}
