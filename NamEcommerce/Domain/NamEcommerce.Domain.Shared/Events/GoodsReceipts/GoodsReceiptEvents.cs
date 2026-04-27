namespace NamEcommerce.Domain.Shared.Events.GoodsReceipts;

public sealed record GoodsReceiptSetToPurchaseOrder(Guid GoodsReceiptId, Guid PurchaseOrderId) : DomainEvent;
public sealed record GoodsReceiptRemovedFromPurchaseOrder(Guid GoodsReceiptId, Guid PurchaseOrderId) : DomainEvent;
