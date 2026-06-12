using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

[Serializable]
public sealed class ShortageAggregationModel
{
    public Guid? OrderId { get; set; }
    public Guid? DeliveryNoteId { get; set; }
    public Guid? ProductId { get; set; }
    public bool IncludeSalesOrderScope { get; set; }
    public IList<VendorShortageGroupModel> VendorGroups { get; set; } = [];
    public bool HasShortage => VendorGroups.Any(group => group.Items.Count > 0);
}

[Serializable]
public sealed class VendorShortageGroupModel
{
    public Guid? VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public bool IsNoVendorGroup { get; set; }
    public IList<ShortageAggregationItemModel> Items { get; set; } = [];
    public decimal TotalQuantityToOrder => Items.Sum(item => item.QuantityToOrder);
    public decimal TotalAmount => Items.Sum(item => item.QuantityToOrder * item.UnitCost);
}

[Serializable]
public sealed class ShortageAggregationItemModel
{
    public string OrderCode { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? UnitMeasurementName { get; set; }
    public int QuantityDecimalPlaces { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
    public decimal QuantityToOrder { get; set; }
    public decimal UnitCost { get; set; }
    public bool IsFromPrimaryOrder { get; set; } = true;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public IList<SupplierSuggestionModel> SupplierSuggestions { get; set; } = [];
    public IList<PurchaseOrderShortageAllocationModel> AllocatedFromPurchaseOrders { get; set; } = [];
}

[Serializable]
public sealed class ExistingDraftPurchaseOrderModel
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
}

[Serializable]
public sealed class ExistingDraftPurchaseOrdersResultModel
{
    public IList<ExistingDraftPurchaseOrderModel> Items { get; set; } = [];
}

[Serializable]
public sealed class RelatedPurchaseOrderItemModel
{
    public Guid PurchaseOrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantityOrdered { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal AvailableForAllocation { get; set; }
    public decimal UnitCost { get; set; }
}

[Serializable]
public sealed class RelatedPurchaseOrderModel
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public bool CanAllocate { get; set; }
    public bool CanMerge { get; set; }
    public IList<RelatedPurchaseOrderItemModel> Items { get; set; } = [];
}

[Serializable]
public sealed class RelatedPurchaseOrdersResultModel
{
    public IList<RelatedPurchaseOrderModel> Items { get; set; } = [];
}

[Serializable]
public sealed class CreatedPurchaseOrderResultModel
{
    public Guid PurchaseOrderId { get; set; }
    public string PurchaseOrderCode { get; set; } = string.Empty;
    public bool CreatedNew { get; set; }
    public Guid VendorId { get; set; }
}

[Serializable]
public sealed class CreatePurchaseOrdersFromShortageResultModel : ICommandResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public IList<CreatedPurchaseOrderResultModel> Items { get; set; } = [];
}
