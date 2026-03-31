namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

[Serializable]
public sealed record AddPurchaseOrderItemResultModel
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
