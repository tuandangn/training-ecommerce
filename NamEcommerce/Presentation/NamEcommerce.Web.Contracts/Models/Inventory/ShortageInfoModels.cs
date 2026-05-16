namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed class SupplierSuggestionModel
{
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public decimal? LastUnitPrice { get; set; }
    public bool IsPreferred { get; set; }
}

[Serializable]
public sealed class PurchaseOrderShortageAllocationModel
{
    public Guid PurchaseOrderId { get; set; }
    public string PurchaseOrderCode { get; set; } = string.Empty;
    public decimal AllocatedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public DateTime? ExpectedReceiveDate { get; set; }
}

[Serializable]
public sealed class ShortageInfoItemModel
{
    public string OrderCode { get; set; } = string.Empty;
    public Guid OrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? UnitMeasurementName { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public IList<SupplierSuggestionModel> SupplierSuggestions { get; set; } = [];
    public IList<PurchaseOrderShortageAllocationModel> AllocatedFromPurchaseOrders { get; set; } = [];
}

[Serializable]
public sealed class ShortageInfoModel
{
    public IList<ShortageInfoItemModel> Items { get; set; } = [];
    public bool HasShortage => Items.Count > 0;
    public decimal TotalShortageQuantity => Items.Sum(item => item.ShortageQuantity);
}
