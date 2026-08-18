using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record ReceivedGoodsDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public DateTime? ReceivedOnUtc { get; set; }
    public required decimal ReceivedQuantity { get; init; }
    public decimal QuantityDecimalPlaces { get; set; }
    public required Guid? WarehouseId { get; init; }
    public decimal? TaxRate { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? ActualUnitCost { get; set; }
    public IList<Guid> PictureIds { get; set; } = [];
    public decimal ShippingAmount { get; set; }

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
public sealed record ReceivedGoodsResultDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public required decimal ReceivedQuantity { get; init; }
    public Guid? CreatedGoodsReceiptId { get; init; }
}
