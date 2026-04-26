namespace NamEcommerce.Web.Contracts.Models.GoodsReceipts;

[Serializable]
public sealed record SetGoodsReceiptVendorResultModel
{
    public required bool Success { get; init; }
    public Guid? UpdatedId { get; init; }
    public string? ErrorMessage { get; init; }
}
