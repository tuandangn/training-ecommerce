using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderItemDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required decimal QuantityOrdered { get; init; }

    public decimal UnitCost { get; set; }
    public string? Note { get; set; }

    public void Verify()
    {
        if (QuantityOrdered <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemQuantityMustBePositive");
        if (UnitCost < 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemUnitCostCannotBeNegative");
    }
}

[Serializable]
public sealed record PurchaseOrderItemDto(Guid Id) : BasePurchaseOrderItemDto
{
    public decimal QuantityReceived { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal TotalCost { get; set; }
}

[Serializable]
public sealed record AddPurchaseOrderItemDto : BasePurchaseOrderItemDto;

[Serializable]
public sealed record UpdatePurchaseOrderItemDto : BasePurchaseOrderItemDto
{
    public required Guid PurchaseOrderItemId { get; init; }

    public new void Verify()
    {
        if (PurchaseOrderItemId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemIsNotFound");

        base.Verify();
    }
}

[Serializable]
public sealed record AddPurchaseOrderItemResultDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid CreatedItemId { get; init; }
}

[Serializable]
public sealed record ReceivedGoodsForItemDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public DateTime? ReceivedOnUtc { get; set; }
    public required decimal ReceivedQuantity { get; init; }
    public required Guid? WarehouseId { get; init; }
    public Guid? ReceivedByUserId { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? ActualUnitCost { get; set; }
    public IList<Guid> PictureIds { get; set; } = [];

    public void Verify()
    {
        if (ReceivedOnUtc.HasValue && ReceivedOnUtc > DateTime.UtcNow)
            throw new PurchaseOrderItemDataIsInvalidException("Error.ReceivedDateCannotBeInFuture");
        if (ReceivedQuantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderReceiveQuantityMustBePositive");
        if (SellingPrice.HasValue && SellingPrice.Value < 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderSellingPriceCannotBeNegative");
        if (ActualUnitCost.HasValue && ActualUnitCost.Value < 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemUnitCostCannotBeNegative");
    }
}
[Serializable]
public sealed record ReceivedGoodsForItemResultDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public required decimal ReceivedQuantity { get; init; }
    public Guid? CreatedGoodsReceiptId { get; init; }
}

[Serializable]
public sealed record BulkReceiveGoodsForPurchaseOrderDto(Guid PurchaseOrderId)
{
    public DateTime? ReceivedOnUtc { get; set; }
    public required IList<BulkReceiveGoodsForPurchaseOrderLineDto> Lines { get; init; }
    public Guid? ReceivedByUserId { get; init; }
    public IList<Guid> PictureIds { get; set; } = [];

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
public sealed record BulkReceiveGoodsForPurchaseOrderLineDto
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
public sealed record BulkReceiveGoodsForPurchaseOrderResultDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required IReadOnlyList<Guid> CreatedGoodsReceiptIds { get; init; }
}
