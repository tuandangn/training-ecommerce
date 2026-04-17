namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record ProductPriceHistoryModel
{
    public required IEnumerable<PriceHistoryItemModel> Items { get; init; }

    [Serializable]
    public sealed record PriceHistoryItemModel
    {
        public decimal OldPrice { get; init; }
        public decimal NewPrice { get; init; }
        public decimal OldCostPrice { get; init; }
        public decimal NewCostPrice { get; init; }
        public string? Note { get; init; }
        public DateTime CreatedOnUtc { get; init; }
    }
}
