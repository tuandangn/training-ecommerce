namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed record CreateWarehouseResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }

    public Guid CreatedId { get; set; }
}
