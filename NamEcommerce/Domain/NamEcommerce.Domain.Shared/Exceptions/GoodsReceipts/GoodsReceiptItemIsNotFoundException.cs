namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

[Serializable]
public sealed class GoodsReceiptItemIsNotFoundException(Guid id) : NamEcommerceDomainException("Error.GoodsReceipt.Item.NotFound", id);
