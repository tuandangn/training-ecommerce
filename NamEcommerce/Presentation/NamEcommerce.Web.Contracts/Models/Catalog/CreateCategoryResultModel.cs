namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record CreateCategoryResultModel : ICommandResult
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }

    public Guid CreatedId { get; set; }
}
