using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class UpdatePurchaseOrderCommand : IRequest<UpdatePurchaseOrderResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public required DateTime PlacedOn { get; init; }
    public required Guid VendorId { get; init; }
    public required Guid? WarehouseId { get; init; }

    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Note { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
}
