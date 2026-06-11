using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record StockReservationEntry : AppAggregateEntity
{
    internal StockReservationEntry(Guid productId, Guid warehouseId, decimal quantityDelta, Guid referenceId, string? note) : base(Guid.NewGuid())
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        QuantityDelta = quantityDelta;
        ReferenceId = referenceId;
        Note = note;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public decimal QuantityDelta { get; init; }
    public Guid ReferenceId { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
