using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

namespace NamEcommerce.Domain.Entities.PurchaseOrders;

[Serializable]
public record PurchaseOrderItem : AppEntity
{
    internal PurchaseOrderItem(Guid purchaseOrderId, Guid productId, decimal quantityOrdered, decimal unitCost) : base(Guid.Empty)
    {
        PurchaseOrderId = purchaseOrderId;
        ProductId = productId;
        QuantityOrdered = quantityOrdered;
        UnitCost = unitCost;

        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid PurchaseOrderId { get; private set; }
    public Guid ProductId { get; private set; }

    public decimal QuantityOrdered { get; internal set; }
    public decimal QuantityReceived { get; private set; }
    public decimal UnitCost { get; internal set; }
    public decimal TotalCost => QuantityOrdered * UnitCost;

    public string? Note { get; internal set; }

    public DateTime CreatedOnUtc { get; private set; }

    #region Methods

    internal void AddQuantityReceived(decimal quantityReceived)
    {
        if (quantityReceived < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityReceived), "Quantity received must be greater than or equal to 0");

        if (!CanReceiveQuantity(quantityReceived))
            throw new PurchaseOrderReceiveQuantityExceedsOrderedQuantityException();

        QuantityReceived += quantityReceived;
    }

    internal bool CanReceiveQuantity(decimal receivingQuantity)
        => QuantityReceived + receivingQuantity <= QuantityOrdered;

    #endregion
}
