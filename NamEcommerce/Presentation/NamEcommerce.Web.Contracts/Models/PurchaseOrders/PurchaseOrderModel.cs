namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required DateTime PlacedOn { get; init; }

    public required Guid VendorId { get; init; }
    public string? VendorName { get; set; }
    public string? VendorPhone { get; set; }
    public string? VendorAddress { get; set; }

    public required Guid? WarehouseId { get; init; }
    public string? WarehouseName { get; set; }
    public string? WarehouseAddress { get; set; }

    public int Status { get; init; }
    public string? Note { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }

    public decimal ShippingAmount { get; init; }
    public decimal AccumulatedShippingAmount { get; init; }
    public decimal TotalShipping => ShippingAmount + AccumulatedShippingAmount;

    public decimal TaxAmount { get; init; }
    public decimal AccumulatedTaxAmount { get; init; }
    public decimal TotalTaxAmount => TaxAmount + AccumulatedTaxAmount;

    public decimal TotalAmount { get; init; }
    public decimal SubTotal => TotalAmount - TotalTaxAmount;

    public DateTime CreatedOn { get; init; }

    public IList<ItemModel> Items { get; set; } = [];

    public bool CanModifyInfo { get; init; }
    public bool CanAddItems { get; init; }
    public bool CanReceiveGoods { get; init; }
    public bool CanChangeDate { get; init; }
    public bool CanChangeFees { get; init; }
    public bool CanChangeVendor { get; init; }
    public bool CanAllocation { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public string ProductName { get; set; } = "";
        public string? ProductPicture { get; set; }

        public string? UnitMeasurement { get; set; }
        public int QuantityDecimalPlaces { get; set; }

        public decimal QuantityOrdered { get; set; }
        public decimal UnitCost { get; set; }
        public decimal QuantityReceived { get; set; }
        public decimal TotalCost { get; set; }

        public string? Note { get; set; }

        public decimal RemainingQuantity { get; set; }
        public bool TrackInventory { get; set; }

        public decimal CurrentUnitPrice { get; set; }
    }
}

