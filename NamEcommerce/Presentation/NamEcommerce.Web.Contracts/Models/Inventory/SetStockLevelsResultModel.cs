namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed record SetStockLevelsResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
    public Guid UpdatedId { get; init; }
}
