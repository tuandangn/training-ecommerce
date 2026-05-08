using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Returns;

[Serializable]
public sealed record VendorReturnItem : AppEntity
{
    private VendorReturnItem() : base(Guid.Empty) { }

    internal VendorReturnItem(Guid id, Guid vendorReturnId, Guid productId, string productName,
        Guid? goodsReceiptItemId, decimal requestedQuantity, decimal acceptedQuantity, decimal unitCost)
        : base(id)
    {
        VendorReturnId = vendorReturnId;
        ProductId = productId;
        ProductName = productName;
        GoodsReceiptItemId = goodsReceiptItemId;
        RequestedQuantity = requestedQuantity;
        AcceptedQuantity = acceptedQuantity;
        UnitCost = unitCost;
    }

    public Guid VendorReturnId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>Item phiếu nhập kho gốc — nullable nếu chỉ biết ProductId.</summary>
    public Guid? GoodsReceiptItemId { get; private set; }

    public decimal RequestedQuantity { get; private set; }
    public decimal AcceptedQuantity { get; internal set; }

    /// <summary>Giá vốn tại thời điểm trả — lấy từ AverageCost của InventoryStock.</summary>
    public decimal UnitCost { get; private set; }

    public decimal AcceptedTotal => AcceptedQuantity * UnitCost;
}
