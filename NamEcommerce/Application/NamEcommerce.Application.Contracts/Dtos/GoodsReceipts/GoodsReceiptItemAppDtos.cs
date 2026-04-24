namespace NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;

[Serializable]
public sealed record GoodsReceiptItemAppDto(Guid Id)
{
    public Guid ProductId { get; init; }
    public string? ProductName { get; set; }

    public Guid? WarehouseId { get; init; }
    public string? WarehouseName { get; set; }

    public decimal Quantity { get; init; }
    public decimal? UnitCost { get; init; }
    public bool IsPendingCosting { get; init; }
}

[Serializable]
public sealed record CreateGoodsReceiptItemAppDto
{
    public required Guid ProductId { get; init; }
    public Guid? WarehouseId { get; init; }

    public required decimal Quantity { get; init; }
    public decimal? UnitCost { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (Quantity <= 0)
            return (false, "Error.GoodsReceipt.Item.QuantityMustBePositive");
        if (UnitCost.HasValue && UnitCost < 0)
            return (false, "Error.GoodsReceipt.Item.UnitCostCannotBeNegative");

        return (true, null);
    }
}

[Serializable]
public sealed record SetGoodsReceiptItemUnitCostAppDto
{
    public required Guid GoodsReceiptId { get; init; }
    public required Guid GoodsReceiptItemId { get; init; }
    public required decimal UnitCost { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (UnitCost < 0)
            return (false, "Error.GoodsReceipt.Item.UnitCostCannotBeNegative");

        return (true, null);
    }
}

[Serializable]
public sealed record SetGoodsReceiptItemUnitCostResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}
