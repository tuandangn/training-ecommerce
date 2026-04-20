using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderItemDto
{
    public required Guid PurchaseOrderId { get; set; }
    public required Guid ProductId { get; init; }

    public decimal QuantityOrdered { get; set; }
    public decimal UnitCost { get; set; }

    public string? Note { get; set; }

    public void Verify()
    {
        if (QuantityOrdered <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Item quantity ordered must be greater than 0");
        if (UnitCost < 0)
            throw new PurchaseOrderItemDataIsInvalidException("Item unit cost must be greater than or equal to 0");
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
    public decimal ReceivedQuantity { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Giá bán mới cho sản phẩm (tùy chọn). Nếu null thì giữ nguyên UnitPrice hiện tại của Product.
    /// </summary>
    public decimal? SellingPrice { get; set; }

    public void Verify()
    {
        if (ReceivedQuantity <= 0)
            throw new InvalidOperationException("Received quantity must be greater than 0");
        if (SellingPrice.HasValue && SellingPrice.Value < 0)
            throw new InvalidOperationException("Selling price must be greater than or equal to 0");
    }
}
[Serializable]
public sealed record ReceivedGoodsForItemResultDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public decimal ReceivedQuantity { get; set; }
}
