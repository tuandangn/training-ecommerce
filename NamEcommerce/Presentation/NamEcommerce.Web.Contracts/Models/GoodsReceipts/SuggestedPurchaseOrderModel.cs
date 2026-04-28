namespace NamEcommerce.Web.Contracts.Models.GoodsReceipts;

[Serializable]
public sealed class SuggestedPurchaseOrderModel
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }

    /// <summary>Ngày đặt hàng — local time, dùng để hiển thị trên UI.</summary>
    public required DateTime PlacedOn { get; init; }
    public required Guid VendorId { get; init; }

    /// <summary>Điểm khớp 0–100.</summary>
    public int MatchScore { get; init; }

    /// <summary>true khi MatchScore = 100 — mọi items của GR đều được fulfill đủ.</summary>
    public bool IsFullMatch { get; init; }

    public IList<ItemModel> Items { get; init; } = [];

    [Serializable]
    public sealed record ItemModel
    {
        public required Guid ProductId { get; init; }
        public string? ProductName { get; init; }
        public decimal QuantityOrdered { get; init; }
        public decimal QuantityReceived { get; init; }
        public decimal UnitCost { get; init; }
    }
}
