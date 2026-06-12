namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record DeleteCategoryResultModel : ICommandResult
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
}
