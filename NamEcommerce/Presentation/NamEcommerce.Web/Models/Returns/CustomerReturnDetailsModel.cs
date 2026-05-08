namespace NamEcommerce.Web.Models.Returns;

[Serializable]
public sealed class CustomerReturnDetailsModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public string? Note { get; init; }

    public required int Status { get; init; }
    public required string StatusLabel { get; init; }
    public required DateTime ReturnDate { get; init; }
    public DateTime? ConfirmedOn { get; init; }
    public Guid? GeneratedGoodsReceiptId { get; init; }

    public required DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }

    public IList<ItemModel> Items { get; init; } = [];

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public required string ProductName { get; init; }
        public Guid? DeliveryNoteItemId { get; init; }
        public required decimal RequestedQuantity { get; init; }
        public required decimal AcceptedQuantity { get; init; }
        public required decimal UnitPrice { get; init; }
        public decimal AcceptedTotal => AcceptedQuantity * UnitPrice;
    }
}
