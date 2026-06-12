namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

[Serializable]
public sealed record ReceivePurchaseOrderItemResultModel : ICommandResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal ActualReceivedQuantity { get; init; }
    public Guid? CreatedGoodsReceiptId { get; init; }
}
