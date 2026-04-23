namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

[Serializable]
public sealed class GoodsReceiptItemDataIsInvalidException(string errorCode, params object[] parameters) : NamEcommerceDomainException(errorCode, parameters);
