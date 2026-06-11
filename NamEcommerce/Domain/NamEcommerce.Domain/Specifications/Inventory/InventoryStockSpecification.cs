using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Inventory;

[Serializable]
public sealed class InventoryStockSpecification(Guid productId, Guid warehouseId) 
    : BaseSpecification<InventoryStock>(inventoryStock => inventoryStock.ProductId == productId && inventoryStock.WarehouseId == warehouseId);
