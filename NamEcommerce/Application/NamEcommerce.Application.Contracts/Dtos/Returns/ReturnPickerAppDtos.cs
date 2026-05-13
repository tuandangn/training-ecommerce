namespace NamEcommerce.Application.Contracts.Dtos.Returns;

[Serializable]
public sealed record DeliveryNotePickerAppDto(Guid Id)
{
    public required string Code { get; init; }
    public required DateTime DeliveredOnUtc { get; init; }
    public Guid WarehouseId { get; set; }
}

[Serializable]
public sealed record GoodsReceiptPickerAppDto(Guid Id)
{
    public required string Code { get; init; }
    public required string Label { get; init; }
    public required DateTime ReceivedOnUtc { get; init; }
    public string? PurchaseOrderCode { get; init; }
    public IReadOnlyList<Guid> WarehouseIds { get; init; } = [];
    public IReadOnlyList<string> WarehouseNames { get; init; } = [];
    public int ItemCount { get; init; }
    public decimal TotalQuantity { get; init; }
    public decimal TotalValue { get; init; }
    public bool IsPendingCosting { get; init; }

    public bool IsFullyReturned { get; init; }
}

[Serializable]
public sealed record ReturnableItemAppDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required string Unit { get; init; }
    public required decimal OriginalQty { get; init; }
    public required decimal AlreadyReturnedQty { get; init; }
    public required decimal UnitPrice { get; init; }
    public Guid? SourceItemId { get; init; }
}
