namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

[Serializable]
public sealed record BulkReceivePurchaseOrderResultModel : ICommandResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>Danh sách GoodsReceipt IDs đã tạo (1 ID nếu cùng kho, &gt;1 nếu chia kho).</summary>
    public IReadOnlyList<Guid> CreatedGoodsReceiptIds { get; init; } = [];
}
