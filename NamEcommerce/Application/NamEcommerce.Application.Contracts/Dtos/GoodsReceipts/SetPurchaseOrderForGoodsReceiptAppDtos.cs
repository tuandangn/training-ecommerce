namespace NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;

[Serializable]
public sealed record SetGoodsReceiptToPurchaseOrderAppDto(Guid Id, Guid PurchaseOrderId);

[Serializable]
public sealed record RemoveGoodsReceiptFromPurchaseOrderAppDto(Guid Id);

/// <summary>
/// PurchaseOrder được gợi ý là phù hợp với GoodsReceipt, dành cho Presentation layer.
/// DateTime đã được convert sang local time.
/// </summary>
[Serializable]
public sealed record SuggestedPurchaseOrderForGoodsReceiptAppDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }

    /// <summary>Ngày đặt hàng — local time.</summary>
    public required DateTime PlacedOn { get; init; }
    public required Guid VendorId { get; init; }

    /// <summary>Điểm khớp 0–100. 100 = tất cả items của GR đều được fulfill đủ số lượng.</summary>
    public int MatchScore { get; init; }

    /// <summary>true khi MatchScore = 100.</summary>
    public bool IsFullMatch { get; init; }

    public IList<SuggestedPurchaseOrderItemForGoodsReceiptAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record SuggestedPurchaseOrderItemForGoodsReceiptAppDto
{
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public decimal QuantityOrdered { get; init; }
    public decimal QuantityReceived { get; init; }
    public decimal UnitCost { get; init; }
}
