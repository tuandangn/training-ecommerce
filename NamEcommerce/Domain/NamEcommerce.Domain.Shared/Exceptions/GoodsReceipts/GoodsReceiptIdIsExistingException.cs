namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

[Serializable]
public sealed class GoodsReceiptIdIsExistingException(Guid id) : NamEcommerceDomainException("Error.GoodReceipt.IdExisting", id);
