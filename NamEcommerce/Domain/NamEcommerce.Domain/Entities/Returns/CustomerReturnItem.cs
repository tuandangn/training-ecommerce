using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Returns;

[Serializable]
public sealed record CustomerReturnItem : AppEntity
{
    private CustomerReturnItem() : base(Guid.Empty) { }

    internal CustomerReturnItem(Guid id, Guid customerReturnId, Guid productId, string productName,
        Guid? deliveryNoteItemId, decimal requestedQuantity, decimal acceptedQuantity, decimal unitPrice)
        : base(id)
    {
        CustomerReturnId = customerReturnId;
        ProductId = productId;
        ProductName = productName;
        DeliveryNoteItemId = deliveryNoteItemId;
        RequestedQuantity = requestedQuantity;
        AcceptedQuantity = acceptedQuantity;
        UnitPrice = unitPrice;
    }

    public Guid CustomerReturnId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>Item phiếu giao hàng tương ứng — nullable nếu không truy được gốc item.</summary>
    public Guid? DeliveryNoteItemId { get; private set; }

    public decimal RequestedQuantity { get; private set; }
    public decimal AcceptedQuantity { get; internal set; }
    public decimal UnitPrice { get; private set; }

    public decimal AcceptedTotal => AcceptedQuantity * UnitPrice;
}
