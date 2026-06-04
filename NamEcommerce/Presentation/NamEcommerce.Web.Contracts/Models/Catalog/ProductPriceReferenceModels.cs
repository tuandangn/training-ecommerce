namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record ProductSalePriceReferenceModel
{
    public required Guid ProductId { get; init; }
    public Guid? CustomerId { get; init; }
    public decimal ProductDefaultUnitPrice { get; init; }
    public decimal SuggestedPrice { get; init; }
    public string Source { get; init; } = "ProductDefault";
    public string SourceText { get; init; } = "Giá bán mặc định";
    public bool RequiresManualInput { get; init; }
    public IList<SalePriceReferenceItemModel> Items { get; init; } = [];

    [Serializable]
    public sealed record SalePriceReferenceItemModel
    {
        public required Guid CustomerId { get; init; }
        public string? CustomerName { get; init; }
        public required decimal UnitPrice { get; init; }
        public required string OrderCode { get; init; }
        public required DateTime OrderDate { get; init; }
        public required string OrderDateText { get; init; }
        public string Source { get; init; } = "CustomerLastSale";
        public string SourceText { get; init; } = "Đơn bán gần nhất";
    }
}

[Serializable]
public sealed record ProductPurchasePriceReferenceModel
{
    public required Guid ProductId { get; init; }
    public Guid? VendorId { get; init; }
    public decimal SuggestedCost { get; init; }
    public string Source { get; init; } = "NoVendorHistory";
    public string SourceText { get; init; } = "Chưa có lịch sử nhập";
    public bool RequiresManualInput { get; init; }
    public IList<PurchasePriceReferenceItemModel> Items { get; init; } = [];

    [Serializable]
    public sealed record PurchasePriceReferenceItemModel
    {
        public Guid? VendorId { get; init; }
        public string? VendorName { get; init; }
        public required decimal UnitCost { get; init; }
        public required string PurchaseOrderCode { get; init; }
        public required DateTime PurchaseDate { get; init; }
        public required string PurchaseDateText { get; init; }
        public string Source { get; init; } = "VendorLastPurchase";
        public string SourceText { get; init; } = "Đơn nhập gần nhất";
    }
}
