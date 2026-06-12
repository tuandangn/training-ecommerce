using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;

[Serializable]
public sealed class QuickCreateAndLinkPurchaseOrderCommand : ICommand<QuickCreateAndLinkPurchaseOrderResultModel>
{
    public required Guid GoodsReceiptId { get; init; }

    /// <summary>
    /// Vendor cho PO. Nếu GoodsReceipt đã có Vendor, handler sẽ ưu tiên dùng vendor của GR
    /// (đảm bảo PO tạo ra trùng vendor với phiếu nhập). Field này dùng khi GR chưa có vendor.
    /// </summary>
    public Guid? VendorId { get; init; }

    public Guid? WarehouseId { get; init; }

    public string? Note { get; init; }

    /// <summary>Đơn giá nhập do user nhập cho các item đang ở trạng thái IsPendingCosting.</summary>
    public IDictionary<Guid, decimal> ItemUnitCosts { get; init; } = new Dictionary<Guid, decimal>();

    /// <summary>Thuế (chi phí phát sinh), tùy chọn. Default 0.</summary>
    public decimal TaxAmount { get; init; }

    /// <summary>Phí giao hàng (chi phí phát sinh), tùy chọn. Default 0.</summary>
    public decimal ShippingAmount { get; init; }
}
