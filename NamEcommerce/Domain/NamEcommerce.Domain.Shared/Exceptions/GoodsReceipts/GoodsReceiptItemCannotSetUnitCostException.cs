namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

[Serializable]
public sealed class GoodsReceiptItemCannotSetUnitCostException() : NamEcommerceDomainException("Error.GoodsReceipt.Item.CannotSetUnitCost");
