namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

/// <summary>
/// Thrown when attempting to delete a GoodsReceipt that has at least one Confirmed VendorReturn.
/// The return must be processed before the goods receipt can be deleted.
/// </summary>
public sealed class GoodsReceiptHasConfirmedReturnsException : NamEcommerceDomainException
{
    public GoodsReceiptHasConfirmedReturnsException(Guid goodsReceiptId)
        : base("Error.GoodsReceiptHasConfirmedReturns", goodsReceiptId)
    {
    }
}
