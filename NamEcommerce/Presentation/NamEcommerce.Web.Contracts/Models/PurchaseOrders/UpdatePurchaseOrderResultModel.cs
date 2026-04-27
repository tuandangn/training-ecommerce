namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record UpdatePurchaseOrderResultModel
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
