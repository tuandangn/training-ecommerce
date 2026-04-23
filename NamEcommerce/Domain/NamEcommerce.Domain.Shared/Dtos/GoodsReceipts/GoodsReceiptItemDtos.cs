using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

namespace NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

[Serializable]
public record BaseGoodsReceiptItemDto
{
    public required Guid ProductId { get; init; }
    public required Guid? WarehouseId { get; init; }

    public required decimal Quantity { get; init; }
    public decimal? UnitCost { get; set; }

    public void Verify()
    {
        if (Quantity <= 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.QuantityMustBePositive");

        if (UnitCost.HasValue && UnitCost < 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.UnitCostCannotBeNegative");
    }
}

[Serializable]
public sealed record GoodsReceiptItemDto(Guid Id) : BaseGoodsReceiptItemDto
{
    public string? ProductName { get; set; }
    public string? WarehouseName { get; set; }
}

[Serializable]
public sealed record AddGoodsReceiptItemDto : BaseGoodsReceiptItemDto;

[Serializable]
public sealed record SetGoodsReceiptItemUnitCostDto
{
    public required Guid GoodsReceiptId { get; init; }
    public required Guid GoodsReceiptItemId { get; init; }
    public required decimal UnitCost { get; set; }

    public void Verify()
    {
        if (UnitCost < 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.UnitCostCannotBeNegative");
    }
}
