using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record ProductPriceHistory : AppAggregateEntity
{
    internal ProductPriceHistory(Guid productId, decimal oldPrice, decimal newPrice, decimal oldCostPrice, decimal newCostPrice, string reason) : base(Guid.NewGuid())
    {
        ProductId = productId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        OldCostPrice = oldCostPrice;
        NewCostPrice = newCostPrice;
        Note = reason;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal OldCostPrice { get; init; }
    public decimal NewCostPrice { get; init; }
    public string Note { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
