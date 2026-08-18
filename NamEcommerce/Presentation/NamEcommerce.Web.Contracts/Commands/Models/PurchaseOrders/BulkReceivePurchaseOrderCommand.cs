using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class BulkReceivePurchaseOrderCommand : ICommand<BulkReceivePurchaseOrderResultModel>
{
    public DateTime? ReceivedOnUtc { get; set; }
    public required Guid PurchaseOrderId { get; init; }
    public IList<BulkReceiveLineCommand> Items { get; init; } = [];
    public decimal ShippingAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public IList<Guid> PictureIds { get; init; } = [];
}

[Serializable]
public sealed class BulkReceiveLineCommand
{
    public required Guid ItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required Guid? WarehouseId { get; init; }
    public decimal? ActualUnitCost { get; init; }
    public Guid? DirectShipOrderId { get; init; }
    public Guid? DirectShipOrderItemId { get; init; }
    public string? DirectShipAddress { get; init; }
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
    public Guid? DirectShipExistingAllocationId { get; init; }
    public int QuantityDecimalPlaces { get; init; }
}
