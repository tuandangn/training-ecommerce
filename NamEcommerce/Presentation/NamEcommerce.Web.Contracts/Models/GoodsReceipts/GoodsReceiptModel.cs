namespace NamEcommerce.Web.Contracts.Models.GoodsReceipts;

[Serializable]
public sealed class GoodsReceiptModel
{
    public required Guid Id { get; init; }
    public required DateTime CreatedOn { get; init; }
    public string? TruckDriverName { get; init; }
    public string? TruckNumberSerial { get; init; }
    public IEnumerable<Guid> PictureIds { get; init; } = [];
    public string? Note { get; init; }
    public bool IsPendingCosting { get; init; }

    // Vendor snapshot
    public Guid? VendorId { get; init; }
    public string? VendorName { get; init; }
    public string? VendorPhone { get; init; }
    public string? VendorAddress { get; init; }

    // Vendor debt linkage — đã sinh nợ NCC từ phiếu này hay chưa.
    // Idempotency của CreateDebtFromGoodsReceiptAsync đảm bảo có tối đa 1 phiếu nợ / 1 phiếu nhập.
    public bool HasVendorDebt { get; init; }
    public Guid? VendorDebtId { get; init; }
    public decimal? VendorDebtTotalAmount { get; init; }

    public IList<ItemModel> Items { get; init; } = [];

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public string? ProductName { get; init; }
        public Guid? WarehouseId { get; init; }
        public string? WarehouseName { get; init; }
        public decimal Quantity { get; init; }
        public decimal? UnitCost { get; init; }
        public bool IsPendingCosting { get; init; }
    }
}
