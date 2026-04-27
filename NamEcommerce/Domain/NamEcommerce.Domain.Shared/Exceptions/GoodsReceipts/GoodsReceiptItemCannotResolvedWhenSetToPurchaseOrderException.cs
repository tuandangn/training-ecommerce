namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

public sealed class GoodsReceiptItemCannotResolvedWhenSetToPurchaseOrderException(Guid productId, decimal quantity) : NamEcommerceDomainException("Error.GoodsReceipt.ItemCannotResolvedWhenSetToPurchaseOrder", productId, quantity);
