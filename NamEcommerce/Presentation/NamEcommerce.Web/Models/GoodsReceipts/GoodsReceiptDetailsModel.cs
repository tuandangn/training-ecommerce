namespace NamEcommerce.Web.Models.GoodsReceipts;

[Serializable]
public sealed class GoodsReceiptDetailsModel
{
    public required Guid Id { get; init; }
    public required DateTime CreatedOn { get; init; }
    public string? TruckDriverName { get; init; }
    public string? TruckNumberSerial { get; init; }
    public IEnumerable<Guid> PictureIds { get; init; } = [];
    public string? Note { get; init; }

    /// <summary>
    /// True nếu còn ít nhất một item chưa được định giá nhập.
    /// </summary>
    public bool IsPendingCosting { get; init; }

    // ─── Vendor snapshot ──────────────────────────────────────
    public Guid? VendorId { get; init; }
    public string? VendorName { get; init; }
    public string? VendorPhone { get; init; }
    public string? VendorAddress { get; init; }

    // ─── Vendor debt linkage ──────────────────────────────────
    /// <summary>True nếu phiếu này đã sinh ra phiếu nợ NCC tương ứng.</summary>
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

        /// <summary>
        /// True nếu item này chưa có đơn giá nhập.
        /// </summary>
        public bool IsPendingCosting { get; init; }
    }
}
