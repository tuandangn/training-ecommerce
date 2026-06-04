using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class ReceivePurchaseOrderItemCommand : IRequest<ReceivePurchaseOrderItemResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid? WarehouseId { get; set; }
    public required decimal ReceivedQuantity { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? ActualUnitCost { get; set; }
    public string? OversupplyAction { get; set; }
    public Guid? DirectShipOrderId { get; set; }
    public Guid? DirectShipOrderItemId { get; set; }
    public string? DirectShipAddress { get; set; }
    public string? DirectShipContactName { get; set; }
    public string? DirectShipContactPhone { get; set; }
    public Guid? DirectShipExistingAllocationId { get; set; }
    public int QuantityDecimalPlaces { get; set; }
}
