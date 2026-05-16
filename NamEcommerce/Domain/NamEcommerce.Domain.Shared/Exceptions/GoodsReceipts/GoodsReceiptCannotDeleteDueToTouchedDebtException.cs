namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

public sealed class GoodsReceiptCannotDeleteDueToTouchedDebtException : NamEcommerceDomainException
{
    public GoodsReceiptCannotDeleteDueToTouchedDebtException(Guid goodsReceiptId)
        : base("Error.GoodsReceiptCannotDeleteDueToTouchedDebt", goodsReceiptId)
    {
    }
}
