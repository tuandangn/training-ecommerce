namespace NamEcommerce.Web.Contracts.Models.Returns;

[Serializable]
public sealed class CustomerReturnModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }

    public required Guid DeliveryNoteId { get; init; }
    public required string DeliveryNoteCode { get; init; }

    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public string? Note { get; init; }

    /// <summary>Giá trị int của <c>CustomerReturnStatus</c>.</summary>
    public required int Status { get; init; }
    public required DateTime ReturnDate { get; init; }
    public DateTime? ConfirmedOn { get; init; }

    /// <summary>Chi phí phát sinh khi nhận hàng trả (vận chuyển, bồi thường...).</summary>
    public required decimal AdditionalCost { get; init; }

    public Guid? GeneratedGoodsReceiptId { get; init; }

    public required DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }

    public IList<ItemModel> Items { get; init; } = [];

    /// <summary>Số tiền hoàn khách = Σ(AcceptedQty × ReturnUnitPrice) − AdditionalCost (floor 0).</summary>
    public decimal NetRefundAmount => Math.Max(0, Items.Sum(i => i.AcceptedTotal) - AdditionalCost);

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public required string ProductName { get; init; }
        public Guid? DeliveryNoteItemId { get; init; }
        public required decimal RequestedQuantity { get; init; }
        public required decimal AcceptedQuantity { get; init; }

        /// <summary>Giá bán gốc (tham chiếu) — null nếu tạo tự do.</summary>
        public decimal? OriginalUnitPrice { get; init; }

        /// <summary>Giá hoàn trả thực tế.</summary>
        public required decimal ReturnUnitPrice { get; init; }

        public decimal AcceptedTotal => AcceptedQuantity * ReturnUnitPrice;
    }
}
