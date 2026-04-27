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
public sealed record AddPurchaseOrderItemResultDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid CreatedItemId { get; init; }
}

[Serializable]
public sealed record ReceivedGoodsForItemDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public required decimal ReceivedQuantity { get; init; }
    public required Guid? WarehouseId { get; init; }
    public Guid? ReceivedByUserId { get; set; }

    /// <summary>
    /// Giá bán mới cho sản phẩm (tùy chọn). Nếu null thì giữ nguyên UnitPrice hiện tại của Product.
    /// </summary>
    public decimal? SellingPrice { get; set; }

    public void Verify()
    {
        if (ReceivedQuantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderReceiveQuantityMustBePositive");
        if (SellingPrice.HasValue && SellingPrice.Value < 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderSellingPriceCannotBeNegative");
    }
}
[Serializable]
public sealed record ReceivedGoodsForItemResultDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public required decimal ReceivedQuantity { get; init; }
}
