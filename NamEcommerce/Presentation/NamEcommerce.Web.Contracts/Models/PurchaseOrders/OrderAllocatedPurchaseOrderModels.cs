namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

[Serializable]
public sealed record OrderAllocatedPurchaseOrderListModel
{
    public required Guid OrderId { get; init; }
    public IList<OrderAllocatedPurchaseOrderModel> Items { get; init; } = [];
    public bool HasItems => Items.Count > 0;
    public int TotalPurchaseOrderCount => Items.Count;
    public int TotalAllocatedItemCount => Items.Sum(po => po.Items.Count);
}

[Serializable]
public sealed record OrderAllocatedPurchaseOrderModel
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public required int Status { get; init; }
    public required string StatusName { get; init; }
    public required string StatusClass { get; init; }
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required DateTime PlacedOn { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public IList<OrderAllocatedPurchaseOrderItemModel> Items { get; init; } = [];
    public int ItemCount => Items.Count;
    public decimal TotalAllocatedQuantity => Items.Sum(item => item.AllocatedQuantity);
    public decimal TotalReceivedQuantity => Items.Sum(item => item.ReceivedQuantity);
    public decimal PendingQuantity => Items.Sum(item => item.PendingQuantity);
    public bool IsFullyReceived => TotalAllocatedQuantity > 0 && PendingQuantity == 0;
}

[Serializable]
public sealed record OrderAllocatedPurchaseOrderItemModel
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public decimal PendingQuantity => Math.Max(0, AllocatedQuantity - ReceivedQuantity);
}
