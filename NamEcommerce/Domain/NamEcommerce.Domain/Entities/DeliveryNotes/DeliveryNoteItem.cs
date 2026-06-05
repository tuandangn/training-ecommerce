using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.DeliveryNotes;

public sealed record DeliveryNoteItem : AppEntity
{
    public DeliveryNoteItem(Guid id) : base(id)
    {
        ProductName = string.Empty;
    }

    internal DeliveryNoteItem(Guid deliveryNoteId, Guid orderItemId, Guid productId, string productName, decimal quantity, decimal unitPrice)
        : this(deliveryNoteId, orderItemId, productId, productName, quantity, unitPrice, Guid.Empty)
    {
    }

    internal DeliveryNoteItem(Guid deliveryNoteId, Guid orderItemId, Guid productId, string productName, decimal quantity, decimal unitPrice, Guid warehouseId) : base(Guid.NewGuid())
    {
        DeliveryNoteId = deliveryNoteId;
        OrderItemId = orderItemId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        WarehouseId = warehouseId;
    }

    public Guid DeliveryNoteId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string ProductName { get; private set; }
    
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Snapshot hiển thị chuyển tiếp. COGS authoritative nằm trong InventoryCostAllocation.
    /// </summary>
    public decimal? CostAtDispatch { get; internal set; }

    public decimal SubTotal => Quantity * UnitPrice;

    // PRE-3: Chiết khấu thương mại (TK 521)
    public decimal DiscountPercent { get; internal set; }   // 0–100
    public decimal DiscountAmount { get; internal set; }    // = SubTotal × DiscountPercent/100

    // PRE-4a: Thuế GTGT đầu ra (TK 3331)
    public decimal? TaxRate { get; internal set; }          // null / 0 / 0.05 / 0.08 / 0.10
    public decimal TaxAmount { get; internal set; }         // = NetAmount × TaxRate

    // Computed — không persist
    public decimal NetAmount => SubTotal - DiscountAmount;
}
