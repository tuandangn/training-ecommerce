using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Returns;

[Serializable]
public sealed record VendorReturnItem : AppEntity
{
    private VendorReturnItem() : base(Guid.Empty) { }

    internal VendorReturnItem(Guid id, Guid vendorReturnId, Guid productId, string productName,
        Guid? goodsReceiptItemId, decimal requestedQuantity, decimal acceptedQuantity,
        decimal? originalUnitCost, decimal returnUnitCost)
        : base(id)
    {
        VendorReturnId = vendorReturnId;
        ProductId = productId;
        ProductName = productName;
        GoodsReceiptItemId = goodsReceiptItemId;
        RequestedQuantity = requestedQuantity;
        AcceptedQuantity = acceptedQuantity;
        OriginalUnitCost = originalUnitCost;
        ReturnUnitCost = returnUnitCost;
    }

    public Guid VendorReturnId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>Item phiếu nhập kho gốc — nullable nếu chỉ biết ProductId hoặc tạo tự do.</summary>
    public Guid? GoodsReceiptItemId { get; private set; }

    public decimal RequestedQuantity { get; private set; }
    public decimal AcceptedQuantity { get; internal set; }

    /// <summary>Giá vốn gốc tại thời điểm nhập (tham chiếu) — null nếu tạo tự do.</summary>
    public decimal? OriginalUnitCost { get; private set; }

    /// <summary>Giá NCC hoàn trả thực tế — có thể khác giá nhập gốc.</summary>
    public decimal ReturnUnitCost { get; private set; }

    public decimal AcceptedTotal => AcceptedQuantity * ReturnUnitCost;
}
