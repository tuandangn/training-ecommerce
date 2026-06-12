namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

[Serializable]
public sealed record QuickCreatePurchaseOrderResultModel : ICommandResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }
    public IReadOnlyList<Guid> GoodsReceiptIds { get; init; } = [];
}
