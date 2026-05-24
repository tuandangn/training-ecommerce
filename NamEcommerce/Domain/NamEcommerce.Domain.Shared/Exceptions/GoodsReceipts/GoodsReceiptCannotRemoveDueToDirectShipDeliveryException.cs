namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

public sealed class GoodsReceiptCannotRemoveDueToDirectShipDeliveryException(Guid goodsReceiptId)
    : NamEcommerceDomainException("Error.GoodsReceiptCannotRemoveDueToDirectShipDelivery", goodsReceiptId);
