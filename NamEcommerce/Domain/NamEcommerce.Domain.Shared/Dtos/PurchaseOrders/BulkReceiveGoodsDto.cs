using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record BulkReceiveGoodsDto(Guid PurchaseOrderId)
{
    public DateTime? ReceivedOnUtc { get; set; }
    public required IList<BulkReceiveGoodsLineDto> Lines { get; init; }
    public Guid? ReceivedByUserId { get; init; }
    public IList<Guid> PictureIds { get; set; } = [];
    public decimal? TaxRate { get; set; }
    public decimal? ShippingAmount { get; set; }

    public void Verify()
    {
        if (PurchaseOrderId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderIsNotFound");
        if (ReceivedOnUtc.HasValue && ReceivedOnUtc > DateTime.UtcNow)
            throw new PurchaseOrderItemDataIsInvalidException("Error.ReceivedDateCannotBeInFuture");
        if (Lines is null || Lines.Count == 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.BulkReceive.NoItemsProvided");
        foreach (var line in Lines)
            line.Verify();
    }
}

[Serializable]
public sealed record BulkReceiveGoodsLineDto
{
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal ReceivedQuantity { get; init; }

    /// <summary>Giá vốn thực tế khi nhận — override PO item cost. Null = dùng PO item cost.</summary>
    public decimal? ActualUnitCost { get; init; }

    public void Verify()
    {
        if (PurchaseOrderItemId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemIsNotFound");
        if (WarehouseId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.WarehouseRequired");
        if (ReceivedQuantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderReceiveQuantityMustBePositive");
        if (ActualUnitCost.HasValue && ActualUnitCost.Value < 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemUnitCostCannotBeNegative");
    }
}

[Serializable]
public sealed record BulkReceiveGoodsResultDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required IReadOnlyList<Guid> CreatedGoodsReceiptIds { get; init; }
}
