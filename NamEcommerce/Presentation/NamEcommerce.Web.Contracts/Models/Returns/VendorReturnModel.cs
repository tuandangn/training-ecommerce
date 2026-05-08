namespace NamEcommerce.Web.Contracts.Models.Returns;

[Serializable]
public sealed class VendorReturnModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public Guid? GoodsReceiptId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public string? Note { get; init; }

    /// <summary>Giá trị int của <c>VendorReturnStatus</c>.</summary>
    public required int Status { get; init; }
    public required DateTime ReturnDate { get; init; }
    public DateTime? ConfirmedOn { get; init; }
    public Guid? GeneratedDeliveryNoteId { get; init; }

    public required DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }

    public IList<ItemModel> Items { get; init; } = [];

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public required string ProductName { get; init; }
        public Guid? GoodsReceiptItemId { get; init; }
        public required decimal RequestedQuantity { get; init; }
        public required decimal AcceptedQuantity { get; init; }
        public required decimal UnitCost { get; init; }
        public decimal AcceptedTotal => AcceptedQuantity * UnitCost;
    }
}
