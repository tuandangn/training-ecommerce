namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record UpdateCategoryResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }

    public Guid UpdatedId { get; init; }
}
