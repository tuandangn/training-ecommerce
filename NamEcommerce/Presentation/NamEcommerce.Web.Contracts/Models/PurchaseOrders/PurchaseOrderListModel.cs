using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderListModel
{
    public string? Keywords { get; init; }
    public int? Status { get; set; }
    public required IPagedDataModel<PurchaseModel> Data { get; init; }

    [Serializable]
    public sealed record PurchaseModel(Guid Id)
    {
        public required DateTime PlacedOn { get; set; }
        public required string Code { get; init; }
        public Guid VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? VendorPhone { get; set; }
        public string? WarehouseName { get; set; }
        public int Status { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime CreatedOn { get; set; }

        public IList<ItemSummaryModel> Items { get; set; } = [];
        public decimal TotalOrdered { get; set; }
        public decimal TotalReceived { get; set; }
    }
}

[Serializable]
public sealed record ItemSummaryModel
{
    public required string ProductName { get; init; }
    public required decimal QuantityOrdered { get; init; }
    public required decimal QuantityReceived { get; init; }
}