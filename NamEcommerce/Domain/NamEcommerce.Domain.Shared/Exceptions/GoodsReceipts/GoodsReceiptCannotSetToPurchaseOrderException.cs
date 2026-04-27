namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

[Serializable]
public sealed class GoodsReceiptCannotSetToPurchaseOrderException() : NamEcommerceDomainException("Error.GoodsReceipt.CannotSetToPurchaseOrder");
