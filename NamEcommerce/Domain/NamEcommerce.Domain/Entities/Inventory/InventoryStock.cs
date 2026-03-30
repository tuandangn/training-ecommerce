using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record InventoryStock : AppAggregateEntity
{
    internal InventoryStock(Guid id, Guid productId, Guid warehouseId, Guid unitMeasurementId) : base(id)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        UnitMeasurementId = unitMeasurementId;
        
        QuantityOnHand = 0;
        QuantityReserved = 0;
        
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid UnitMeasurementId { get; init; }
    
    // Vị trí cụ thể trong kho, bỏ qua ở Phase 1 cho đơn giản, nullable
    public Guid? WarehouseZoneId { get; internal set; }    

    public decimal QuantityOnHand { get; internal set; }
    public decimal QuantityReserved { get; internal set; }
    
    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;
    
    public decimal ReorderLevel { get; internal set; }
    public decimal MaxStockLevel { get; internal set; }
    
    public DateTime? LastStocktakeDate { get; internal set; }
    
    public DateTime UpdatedOnUtc { get; internal set; }
}
