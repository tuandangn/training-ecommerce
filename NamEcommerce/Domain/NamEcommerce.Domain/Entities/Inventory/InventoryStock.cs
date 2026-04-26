using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record InventoryStock : AppAggregateEntity
{
    internal InventoryStock(Guid id, Guid productId, Guid warehouseId, Guid? unitMeasurementId) : base(id)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        UnitMeasurementId = unitMeasurementId;

        QuantityOnHand = 0;
        QuantityReserved = 0;
        AverageCost = 0;

        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid? UnitMeasurementId { get; init; }

    public Guid? WarehouseZoneId { get; internal set; }

    public decimal QuantityOnHand { get; internal set; }
    public decimal QuantityReserved { get; internal set; }

    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;

    /// <summary>
    /// Giá vốn bình quân, tính lại bằng Full Recalculation mỗi khi một GoodsReceiptItem được set UnitCost
    /// cho cùng (ProductId, WarehouseId): SUM(qty × unitCost) / SUM(qty) trên các item đã có UnitCost.
    /// Trong giai đoạn còn item chưa định giá, giá trị này chỉ phản ánh phần đã biết giá → UI cần
    /// cảnh báo người dùng (xem IsPendingCosting()).
    /// </summary>
    public decimal AverageCost { get; internal set; }

    public decimal ReorderLevel { get; internal set; }
    public decimal MaxStockLevel { get; internal set; }
    
    public DateTime? LastStocktakeDate { get; internal set; }
    
    /// <summary>
    /// Tracks when the current reservation expires. Auto-release if exceeded.
    /// Null means no active reservation or unrestricted reservation.
    /// </summary>
    public DateTime? ReservedUntilUtc { get; internal set; }
    
    public DateTime UpdatedOnUtc { get; internal set; }
}

