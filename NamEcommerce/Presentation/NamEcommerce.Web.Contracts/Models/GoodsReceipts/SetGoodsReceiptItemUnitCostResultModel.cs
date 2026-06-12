namespace NamEcommerce.Web.Contracts.Models.GoodsReceipts;

[Serializable]
public sealed record SetGoodsReceiptItemUnitCostResultModel : ICommandResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
