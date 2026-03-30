using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record StockMovementLog : AppAggregateEntity
{
    internal StockMovementLog(Guid id, Guid productId, Guid warehouseId, 
        StockMovementType movementType, decimal quantity, decimal quantityBefore, decimal quantityAfter,
        StockReferenceType referenceType, Guid? referenceId, string? note, Guid createdByUserId) : base(id)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        MovementType = movementType;
        Quantity = quantity;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityAfter;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Note = note;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    
    public StockMovementType MovementType { get; init; }
    
    public decimal Quantity { get; init; }
    public decimal QuantityBefore { get; init; }
    public decimal QuantityAfter { get; init; }
    
    public StockReferenceType ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    
    public string? Note { get; init; }
    
    public Guid CreatedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
