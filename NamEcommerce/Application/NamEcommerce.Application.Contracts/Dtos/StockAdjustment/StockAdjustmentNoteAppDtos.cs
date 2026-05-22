namespace NamEcommerce.Application.Contracts.Dtos.StockAdjustment;

[Serializable]
public sealed record StockAdjustmentNoteAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public string? Note { get; init; }
    public required int Status { get; init; }
    public DateTime? ApprovedOnUtc { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public IList<StockAdjustmentNoteItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record StockAdjustmentNoteItemAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal SystemQuantity { get; init; }
    public required decimal PhysicalQuantity { get; init; }
    public decimal Delta => PhysicalQuantity - SystemQuantity;
}

[Serializable]
public sealed record CreateStockAdjustmentNoteAppDto
{
    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }
    public required IList<CreateStockAdjustmentNoteItemAppDto> Items { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (WarehouseId == Guid.Empty)
            return (false, "Vui lòng chọn kho.");
        if (Items == null || Items.Count == 0)
            return (false, "Phiếu kiểm kê phải có ít nhất một mặt hàng.");
        if (Items.Any(i => i.ProductId == Guid.Empty))
            return (false, "Hàng hóa không hợp lệ.");
        if (Items.Any(i => i.PhysicalQuantity < 0))
            return (false, "Số lượng thực tế không được âm.");
        return (true, null);
    }
}

[Serializable]
public sealed record CreateStockAdjustmentNoteItemAppDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal SystemQuantity { get; init; }
    public required decimal PhysicalQuantity { get; init; }
}

[Serializable]
public sealed record CreateStockAdjustmentNoteResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; init; }
}

[Serializable]
public sealed record StockAdjustmentNoteListAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required int Status { get; init; }
    public int ItemCount { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}
