using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IInventoryCostingManager
{
    Task<InventoryCostMovementResultDto> RegisterInboundAsync(RegisterInventoryInboundCostDto dto);
    Task<InventoryCostMovementResultDto> RegisterOutboundAsync(RegisterInventoryOutboundCostDto dto);
    Task<InventoryCostMovementResultDto> RegisterReceiptReversalAsync(RegisterInventoryReceiptReversalCostDto dto);
    Task<InventoryCostMovementResultDto> RegisterTransferInAsync(RegisterInventoryTransferInCostDto dto);
    Task AssignGoodsReceiptItemCostAsync(AssignGoodsReceiptItemCostDto dto);
    Task<InventoryCostSummaryDto> GetCurrentCostSummaryAsync(Guid productId);
    Task<InventoryCogsSummaryDto> GetCogsForReferencesAsync(InventoryCostReferenceType referenceType, IEnumerable<Guid> referenceIds);
    Task RegisterSaleDispatchReversalAsync(RegisterSaleDispatchReversalDto dto);
    Task<Guid> RevalueProductFromAsync(Guid productId, DateTime fromUtc, InventoryCostRebuildTrigger trigger, Guid? requestedByUserId);
    Task<Guid> RebuildAllAsync(InventoryCostingMethod method, InventoryValuationScope scope, Guid? requestedByUserId);
}
