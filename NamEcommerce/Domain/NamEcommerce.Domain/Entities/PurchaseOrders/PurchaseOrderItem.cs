using NamEcommerce.Domain.Shared;

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

    public Guid PurchaseOrderId { get; }
    public Guid ProductId { get; }

    public decimal QuantityOrdered { get; internal set; }
    public decimal UnitCost { get; internal set; }
    public decimal QuantityReceived { get; private set; }
    public decimal TotalCost => QuantityOrdered * UnitCost;

    public string? Note { get; internal set; }

    public DateTime CreatedOnUtc { get; }

    #region Methods

    public void AddQuantityReceived(decimal quantityReceived)
    {
        if (quantityReceived < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityReceived), "Quantity received must be greater than or equal to 0");

        QuantityReceived += quantityReceived;
    }

    #endregion
}
