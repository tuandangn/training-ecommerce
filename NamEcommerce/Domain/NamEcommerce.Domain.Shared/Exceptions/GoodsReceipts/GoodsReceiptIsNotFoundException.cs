namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

[Serializable]
public sealed class GoodsReceiptIsNotFoundException(Guid id) : NamEcommerceDomainException("Error.GoodsReceipt.NotFound", id);
