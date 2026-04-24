namespace NamEcommerce.Web.Contracts.Models.GoodsReceipts;

[Serializable]
public sealed record UpdateGoodsReceiptResultModel
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? UpdatedId { get; init; }
}
