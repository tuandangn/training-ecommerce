using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Returns;

[Serializable]
public sealed record CustomerReturnItem : AppEntity
{
    private CustomerReturnItem() : base(Guid.Empty) { }

    internal CustomerReturnItem(Guid id, Guid customerReturnId, Guid productId, string productName,
        Guid? deliveryNoteItemId, decimal requestedQuantity, decimal acceptedQuantity,
        decimal? originalUnitPrice, decimal returnUnitPrice)
        : base(id)
    {
        CustomerReturnId = customerReturnId;
        ProductId = productId;
        ProductName = productName;
        DeliveryNoteItemId = deliveryNoteItemId;
        RequestedQuantity = requestedQuantity;
        AcceptedQuantity = acceptedQuantity;
        OriginalUnitPrice = originalUnitPrice;
        ReturnUnitPrice = returnUnitPrice;
    }

    public Guid CustomerReturnId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>Item phiếu giao hàng tương ứng — nullable nếu không truy được gốc item hoặc tạo tự do.</summary>
    public Guid? DeliveryNoteItemId { get; private set; }

    public decimal RequestedQuantity { get; private set; }
    public decimal AcceptedQuantity { get; internal set; }

    /// <summary>Giá bán gốc từ phiếu xuất (tham chiếu) — null nếu tạo tự do.</summary>
    public decimal? OriginalUnitPrice { get; private set; }

    /// <summary>Giá trả về thực tế (có thể thấp hơn giá gốc do khấu hao, hư hỏng...).</summary>
    public decimal ReturnUnitPrice { get; private set; }

    public decimal AcceptedTotal => AcceptedQuantity * ReturnUnitPrice;
}
