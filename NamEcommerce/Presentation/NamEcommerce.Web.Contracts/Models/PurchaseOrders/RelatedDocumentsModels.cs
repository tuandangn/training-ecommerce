namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

/// <summary>Compact view của một phiếu nhận hàng (GoodsReceipt) liên quan đến đơn nhập.</summary>
[Serializable]
public sealed record RelatedGoodsReceiptModel(Guid Id)
{
    public required string Code { get; set; }
    public required DateTime ReceivedOn { get; init; }
    public required int ItemCount { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required bool IsPendingCosting { get; init; }
    public decimal? TotalValue { get; init; }
    public IList<string> WarehouseNames { get; init; } = [];
    public IList<RelatedGoodsReceiptItemModel> Items { get; init; } = [];
}

[Serializable]
public sealed record RelatedGoodsReceiptItemModel
{
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public string? WarehouseName { get; init; }
    public decimal? UnitCost { get; init; }
}

/// <summary>Compact view của một phiếu trả hàng NCC (VendorReturn) liên quan đến đơn nhập.</summary>
[Serializable]
public sealed record RelatedVendorReturnModel(Guid Id)
{
    public required string Code { get; init; }
    public required DateTime ReturnDate { get; init; }
    public required int Status { get; init; }
    public required string WarehouseName { get; init; }
    public required int ItemCount { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal TotalAmount { get; init; }
    public decimal AdditionalCost { get; init; }

    public IList<RelatedVendorReturnItemModel> Items { get; init; } = [];
}

[Serializable]
public sealed record RelatedVendorReturnItemModel
{
    public required string ProductName { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal ReturnUnitCost { get; init; }
}
