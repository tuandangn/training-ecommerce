namespace NamEcommerce.Application.Contracts.Dtos.Inventory;

[Serializable]
public sealed record ShortageAggregationFilterAppDto
{
    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? ProductId { get; init; }
    public bool IncludeSalesOrderScope { get; init; }
}
[Serializable]
public sealed record DeliveryNoteShortageAggregationFilterAppDto
{
    public Guid DeliveryNoteId { get; init; }
    public Guid? ProductId { get; init; }
}
[Serializable]
public sealed record OrderShortageAggregationFilterAppDto
{
    public Guid OrderId { get; init; }
    public Guid? ProductId { get; init; }
}


[Serializable]
public sealed record PurchaseOrderShortageAllocationAppDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public DateTime? ExpectedReceiveDateUtc { get; init; }
}

[Serializable]
public sealed record SupplierSuggestionAppDto
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public int? DisplayOrder { get; init; }
    public DateTime? LastPurchaseDateUtc { get; init; }
    public decimal? LastUnitPrice { get; init; }
    public bool IsPreferred { get; init; }
}

[Serializable]
public sealed record ShortageAggregationItemAppDto
{
    public required string OrderCode { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public string? UnitMeasurementName { get; init; }
    public int QuantityDecimalPlaces { get; init; }
    public required decimal RequiredQuantity { get; init; }
    public required decimal ShippedQuantity { get; init; }
    public required decimal AvailableQuantity { get; init; }
    public required decimal ShortageQuantity { get; init; }
    public required decimal QuantityToOrder { get; init; }
    public decimal UnitCost { get; init; }
    public bool IsFromPrimaryOrder { get; init; } = true;
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerAddress { get; init; }
    public IList<SupplierSuggestionAppDto> SupplierSuggestions { get; init; } = [];
    public IList<PurchaseOrderShortageAllocationAppDto> AllocatedFromPurchaseOrders { get; init; } = [];
}

[Serializable]
public sealed record VendorShortageGroupAppDto
{
    public Guid? VendorId { get; init; }
    public string VendorName { get; init; } = string.Empty;
    public bool IsNoVendorGroup { get; init; }
    public IList<ShortageAggregationItemAppDto> Items { get; init; } = [];
    public decimal TotalQuantityToOrder => Items.Sum(item => item.QuantityToOrder);
}

[Serializable]
public sealed record ShortageAggregationAppDto
{
    public ShortageAggregationFilterAppDto Filter { get; init; } = new();
    public IList<VendorShortageGroupAppDto> VendorGroups { get; init; } = [];
    public bool HasShortage => VendorGroups.Any(group => group.Items.Count > 0);
}

[Serializable]
public sealed record ExistingDraftPurchaseOrderAppDto
{
    public required Guid Id { get; init; }
    public required Guid VendorId { get; init; }
    public required string Code { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? ExpectedDeliveryDateUtc { get; init; }
}

[Serializable]
public sealed record RelatedPurchaseOrderItemAppDto
{
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal QuantityOrdered { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required decimal AvailableForAllocation { get; init; }
    public required decimal UnitCost { get; init; }
}

[Serializable]
public sealed record RelatedPurchaseOrderAppDto
{
    public required Guid Id { get; init; }
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required string Code { get; init; }
    public required int Status { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? ExpectedDeliveryDateUtc { get; init; }
    public required decimal TotalAmount { get; init; }
    public required int ItemCount { get; init; }
    public required bool CanAllocate { get; init; }
    public required bool CanMerge { get; init; }
    public IList<RelatedPurchaseOrderItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CheckRelatedPurchaseOrdersAppDto
{
    public IList<Guid> VendorIds { get; init; } = [];
    public IList<Guid> ProductIds { get; init; } = [];
}

[Serializable]
public sealed record DirectShipInfoAppDto
{
    public required string Address { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public int Priority { get; init; }
}

[Serializable]
public sealed record CreatePoFromShortageItemAppDto
{
    public required Guid OrderId { get; set; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? AllocationQuantity { get; init; }
    public decimal UnitCost { get; init; }
    public string? Note { get; init; }
    public DirectShipInfoAppDto? DirectShipInfo { get; init; }
    public IList<CreatePoFromShortageActionAppDto> Actions { get; init; } = [];
}

[Serializable]
public sealed record CreatePoFromShortageActionAppDto
{
    public required string ActionType { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public Guid? PurchaseOrderItemId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? UnitCost { get; init; }
}

[Serializable]
public sealed record CreatePoFromShortageGroupAppDto
{
    public Guid? VendorId { get; init; }
    public Guid? WarehouseId { get; init; }
    public Guid? MergeIntoExistingPoId { get; init; }
    public DateTime? ExpectedDeliveryDateUtc { get; init; }
    public string? Note { get; init; }
    public IList<CreatePoFromShortageItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CreatePosFromShortageAppDto
{
    public IList<CreatePoFromShortageGroupAppDto> Groups { get; init; } = [];
}

[Serializable]
public sealed record CreatedPurchaseOrderResultAppDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public required bool CreatedNew { get; init; }
    public required Guid VendorId { get; init; }
}

[Serializable]
public sealed record CreatePosFromShortageResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IList<CreatedPurchaseOrderResultAppDto> Items { get; init; } = [];
}
