namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed record DeleteWarehouseResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
}
