namespace NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

[Serializable]
public sealed record SetGoodsReceiptToPurchaseOrderDto(Guid Id, Guid PurchaseOrderId);

[Serializable]
public sealed record RemoveGoodsReceiptFromPurchaseOrderDto(Guid Id);

/// <summary>
/// PurchaseOrder được gợi ý là phù hợp với GoodsReceipt, kèm điểm khớp.
/// </summary>
[Serializable]
public sealed record SuggestedPurchaseOrderForGoodsReceiptDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public required DateTime PlacedOnUtc { get; init; }
    public required Guid VendorId { get; init; }

    /// <summary>Điểm khớp 0–100. 100 = tất cả items của GR đều được fulfill đủ số lượng.</summary>
    public int MatchScore { get; init; }

    /// <summary>true khi MatchScore = 100 (mọi GR item đều resolve được).</summary>
    public bool IsFullMatch { get; init; }

    public IList<SuggestedPurchaseOrderItemForGoodsReceiptDto> Items { get; init; } = [];
}

[Serializable]
public sealed record SuggestedPurchaseOrderItemForGoodsReceiptDto
{
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public decimal QuantityOrdered { get; init; }
    public decimal QuantityReceived { get; init; }
    public decimal UnitCost { get; init; }
}
