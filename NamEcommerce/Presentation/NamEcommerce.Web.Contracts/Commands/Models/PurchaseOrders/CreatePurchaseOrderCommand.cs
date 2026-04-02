using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class CreatePurchaseOrderCommand : IRequest<CreatePurchaseOrderResultModel>
{
    public required Guid? CreatedByUserId { get; init; }
    public Guid? VendorId { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? Note { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public decimal ShippingAmount { get; set; }
    public decimal TaxAmount { get; set; }
}
