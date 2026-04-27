using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class CreatePurchaseOrderCommand : IRequest<CreatePurchaseOrderResultModel>
{
    public required DateTime PlacedOn { get; init; }
    public required Guid VendorId { get; init; }
    public required Guid? WarehouseId { get; init; }

    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string? Note { get; set; }

    public IList<CreatePurchaseOrderItemCommand> Items { get; set; } = [];
}

[Serializable]
public sealed class CreatePurchaseOrderItemCommand
{
    public Guid? ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
}
