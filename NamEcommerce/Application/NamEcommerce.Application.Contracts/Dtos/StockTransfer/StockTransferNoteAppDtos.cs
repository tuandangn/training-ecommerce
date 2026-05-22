namespace NamEcommerce.Application.Contracts.Dtos.StockTransfer;

[Serializable]
public sealed record StockTransferNoteAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid FromWarehouseId { get; init; }
    public string? FromWarehouseName { get; init; }
    public required Guid ToWarehouseId { get; init; }
    public string? ToWarehouseName { get; init; }
    public string? Note { get; init; }
    public required int Status { get; init; }
    public DateTime? ApprovedOnUtc { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public IList<StockTransferNoteItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record StockTransferNoteItemAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
}

[Serializable]
public sealed record CreateStockTransferNoteAppDto
{
    public required Guid FromWarehouseId { get; init; }
    public required Guid ToWarehouseId { get; init; }
    public string? Note { get; init; }
    public required IList<CreateStockTransferNoteItemAppDto> Items { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (FromWarehouseId == Guid.Empty)
            return (false, "Vui lòng chọn kho nguồn.");
        if (ToWarehouseId == Guid.Empty)
            return (false, "Vui lòng chọn kho đích.");
        if (FromWarehouseId == ToWarehouseId)
            return (false, "Kho nguồn và kho đích không được trùng nhau.");
        if (Items == null || Items.Count == 0)
            return (false, "Phiếu chuyển kho phải có ít nhất một mặt hàng.");
        if (Items.Any(i => i.ProductId == Guid.Empty))
            return (false, "Hàng hóa không hợp lệ.");
        if (Items.Any(i => i.Quantity <= 0))
            return (false, "Số lượng chuyển phải lớn hơn 0.");
        return (true, null);
    }
}

[Serializable]
public sealed record CreateStockTransferNoteItemAppDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record CreateStockTransferNoteResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; init; }
}

[Serializable]
public sealed record StockTransferNoteResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record StockTransferNoteListAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid FromWarehouseId { get; init; }
    public string? FromWarehouseName { get; init; }
    public required Guid ToWarehouseId { get; init; }
    public string? ToWarehouseName { get; init; }
    public required int Status { get; init; }
    public int ItemCount { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}
