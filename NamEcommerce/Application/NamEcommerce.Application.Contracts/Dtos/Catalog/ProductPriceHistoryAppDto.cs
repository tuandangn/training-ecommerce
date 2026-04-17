namespace NamEcommerce.Application.Contracts.Dtos.Catalog;

[Serializable]
public sealed record ProductPriceHistoryAppDto
{
    public Guid Id { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal OldCostPrice { get; init; }
    public decimal NewCostPrice { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
