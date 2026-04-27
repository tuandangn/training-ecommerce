namespace NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

[Serializable]
public sealed record SetGoodsReceiptToPurchaseOrderDto(Guid Id, Guid PurchaseOrderId);

[Serializable]
public sealed record RemoveGoodsReceiptFromPurchaseOrderDto(Guid Id);
