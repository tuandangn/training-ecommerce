namespace NamEcommerce.Web.Contracts.Models.GoodsReceipts;

[Serializable]
public sealed record QuickCreateAndLinkPurchaseOrderResultModel : ICommandResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Id của PurchaseOrder vừa tạo (nếu thành công).</summary>
    public Guid? CreatedPurchaseOrderId { get; set; }
}
