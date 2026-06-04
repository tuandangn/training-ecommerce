using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class BulkReceivePurchaseOrderCommand : IRequest<BulkReceivePurchaseOrderResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public IList<BulkReceiveLineCommand> Items { get; init; } = [];
    public decimal AdditionalShipping { get; set; }
    public decimal AdditionalTax { get; set; }
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
