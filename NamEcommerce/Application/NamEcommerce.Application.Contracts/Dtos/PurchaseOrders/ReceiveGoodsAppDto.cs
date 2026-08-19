using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record ReceiveGoodsAppDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public DateTime? ReceivedOnUtc { get; set; }
    public required decimal ReceivedQuantity { get; init; }
    public decimal QuantityDecimalPlaces { get; set; }
    public required Guid? WarehouseId { get; init; }
    public decimal? TaxRate { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal? ActualUnitCost { get; set; }
    public IList<Guid> PictureIds { get; set; } = [];

    public decimal? SellingPrice { get; set; }
    public Guid? ReceivedByUserId { get; set; }

    public Guid? DirectShipOrderId { get; set; }
    public Guid? DirectShipOrderItemId { get; set; }
    public string? DirectShipAddress { get; set; }
    public string? DirectShipContactName { get; set; }
    public string? DirectShipContactPhone { get; set; }
    public Guid? DirectShipExistingAllocationId { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (ReceivedQuantity <= 0)
            return (false, "Error.PurchaseOrderQuantityMustBePositive");
        if (SellingPrice.HasValue && SellingPrice.Value < 0)
            return (false, "Error.ProductUnitPriceCannotBeNegative");
        if (ActualUnitCost.HasValue && ActualUnitCost.Value < 0)
            return (false, "Error.PurchaseOrderItemUnitCostCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record ReceiveGoodsResultAppDto : CommonActionResultDto
{
    public decimal ActualReceivedQuantity { get; init; }
    public Guid? CreatedGoodsReceiptId { get; init; }

    public static ReceiveGoodsResultAppDto CreateSuccess(decimal actualQty, Guid? createdGoodsReceiptId)
        => new() { Success = true, ActualReceivedQuantity = actualQty, CreatedGoodsReceiptId = createdGoodsReceiptId };

    public static new ReceiveGoodsResultAppDto CreateError(string? errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}
