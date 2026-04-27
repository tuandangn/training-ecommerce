using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderListModel
{
    public string? Keywords { get; init; }
    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required DateTime PlacedOn { get; set; }
        public required string Code { get; init; }
        public string? VendorName { get; set; }
        public string? VendorPhone { get; set; }
        public string? WarehouseName { get; set; }
        public int Status { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}