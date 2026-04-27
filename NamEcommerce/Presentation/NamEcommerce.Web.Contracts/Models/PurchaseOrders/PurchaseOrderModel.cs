namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required DateTime PlacedOn { get; init; }

    public required Guid VendorId { get; set; }
    public string? VendorName { get; set; }
    public string? VendorPhone { get; set; }
    public string? VendorAddress { get; set; }

    public required Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? WarehouseAddress { get; set; }

    public int Status { get; set; }
    public string? Note { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime CreatedOn { get; set; }

    public IList<ItemModel> Items { get; set; } = [];

    public bool CanModifyInfo { get; set; }
    public bool CanAddItems { get; set; }
    public bool CanReceiveGoods { get; set; }
    public bool CanChangeDate { get; set; }
    public bool CanChangeFees { get; set; }
    public bool CanChangeVendor { get; set; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public string ProductName { get; set; } = "";
        public string? ProductPicture { get; set; }

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

