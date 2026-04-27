namespace NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;

[Serializable]
public sealed record SetGoodsReceiptToPurchaseOrderAppDto(Guid Id, Guid PurchaseOrderId);

[Serializable]
public sealed record RemoveGoodsReceiptFromPurchaseOrderAppDto(Guid Id);
